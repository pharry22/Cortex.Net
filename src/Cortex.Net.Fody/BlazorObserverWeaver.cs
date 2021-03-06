﻿// <copyright file="BlazorObserverWeaver.cs" company="Jan-Willem Spuij">
// Copyright 2019 Jan-Willem Spuij
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
// the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>

namespace Cortex.Net.Fody
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using global::Fody;
    using Mono.Cecil;
    using Mono.Cecil.Cil;

    /// <summary>
    /// Weaver for Blazor components decorated with the ObserverAttribute class.
    /// </summary>
    public class BlazorObserverWeaver
    {
        /// <summary>
        /// The prefix for an inner method that is the target of an action.
        /// </summary>
        private const string InnerCounterFieldPrefix = "cortex_Net_Fvclsnf97SxcMxlkizajkz_";

        /// <summary>
        /// The full name of the ComponentBasetype.
        /// </summary>
        private const string ComponentBaseTypeName = "Microsoft.AspNetCore.Components.ComponentBase";

        /// <summary>
        /// The name of an Inner ObserverObject field.
        /// </summary>
        private const string InnerObserverObjectFieldName = "cortex_Net_H90skjHYJKq9_ObserverObject";

        /// <summary>
        /// The processor queue.
        /// </summary>
        private readonly ISharedStateAssignmentILProcessorQueue processorQueue;

        /// <summary>
        /// Weaving context.
        /// </summary>
        private readonly BlazorWeavingContext weavingContext;

        /// <summary>
        /// The parent weaver.
        /// </summary>
        private readonly ModuleWeaver parentWeaver;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlazorObserverWeaver"/> class.
        /// </summary>
        /// <param name="parentWeaver">The parent weaver of this weaver.</param>
        /// <param name="processorQueue">The processor queue to add delegates to, to be executed on ISharedState property assignment.</param>
        /// <param name="weavingContext">The resolved types necessary by this weaver.</param>
        public BlazorObserverWeaver(ModuleWeaver parentWeaver, ISharedStateAssignmentILProcessorQueue processorQueue, BlazorWeavingContext weavingContext)
        {
            this.parentWeaver = parentWeaver ?? throw new ArgumentNullException(nameof(parentWeaver));
            this.processorQueue = processorQueue ?? throw new ArgumentNullException(nameof(processorQueue));
            this.weavingContext = weavingContext ?? throw new ArgumentNullException(nameof(weavingContext));
        }

        /// <summary>
        /// Executes this weaver.
        /// </summary>
        internal void Execute()
        {
            var decoratedClasses = from t in this.parentWeaver.ModuleDefinition.GetTypes()
                                   where
                                      t != null &&
                                      t.IsClass &&
                                      t.BaseType != null &&
                                      t.CustomAttributes != null &&
                                      t.CustomAttributes.Any(x => x.AttributeType.FullName == this.weavingContext.CortexNetBlazorObserverAttribute.FullName)
                                   select t;

            foreach (var decoratedClass in decoratedClasses.ToList())
            {
                var baseType = decoratedClass.BaseType.Resolve();

                while (baseType != null)
                {
                    if (baseType.FullName == ComponentBaseTypeName)
                    {
                        this.WeaveClass(decoratedClass);
                    }

                    baseType = baseType.BaseType?.Resolve();
                }
            }
        }

        /// <summary>
        /// Weave the ComponentBase derived class.
        /// </summary>
        /// <param name="decoratedClass">The derived class to weave.</param>
        private void WeaveClass(TypeDefinition decoratedClass)
        {
            var module = decoratedClass.Module;
            var decoratedType = decoratedClass.Resolve();

            var componentBaseType = decoratedType;

            var stateHasChangedMethod = componentBaseType.Methods.FirstOrDefault(x => x.Name == "StateHasChanged");
            var buildRenderTreeMethod = componentBaseType.Methods.FirstOrDefault(x => x.Name == "BuildRenderTree" && x.Parameters.Count == 1);

            while (stateHasChangedMethod == null)
            {
                if (componentBaseType.BaseType != null)
                {
                    componentBaseType = componentBaseType.BaseType.Resolve();
                    stateHasChangedMethod = componentBaseType.Methods.FirstOrDefault(x => x.Name == "StateHasChanged");
                }
                else
                {
                    this.parentWeaver.WriteWarning(Properties.Resources.StateHasChangedNotFound);
                    return;
                }
            }

            var observerObjectType = this.weavingContext.CortexNetBlazorObserverObject;
            var innerObserverObjectField = decoratedType.CreateField(observerObjectType, InnerObserverObjectFieldName, this.weavingContext);

            // observerName name
            var observerName = decoratedClass.Name;
            var observerAttribute = decoratedClass.CustomAttributes.SingleOrDefault(x => x.AttributeType.FullName == this.weavingContext.CortexNetBlazorObserverAttribute.FullName);

            if (observerAttribute != null)
            {
                foreach (var constructorArgument in observerAttribute.ConstructorArguments)
                {
                    if (constructorArgument.Type.FullName == typeof(string).FullName)
                    {
                        observerName = constructorArgument.Value as string;
                    }
                }
            }

            this.processorQueue.SharedStateAssignmentQueue.Enqueue((decoratedClass, true, (processor, sharedStateBackingField) => this.EmitObserverObjectInit(
                processor,
                observerName,
                innerObserverObjectField,
                buildRenderTreeMethod,
                stateHasChangedMethod)));

            // add entrance counter field.
            var entranceCounterDefinition = decoratedType.CreateField(module.TypeSystem.Int32, $"{InnerCounterFieldPrefix}BuildRenderTree_EntranceCount", this.weavingContext, FieldAttributes.Private);

            // Weave the render tree method.
            this.WeaveBuildRenderTreeMethod(buildRenderTreeMethod, observerObjectType, innerObserverObjectField, entranceCounterDefinition);

            // Add a dispose method.
            this.AddDispose(decoratedType, observerObjectType, innerObserverObjectField);
        }

        /// <summary>
        /// Adds a Dispose method to the Component.
        /// </summary>
        /// <param name="decoratedType">The type of the observer decorated component.</param>
        /// <param name="observerObjectType">The type of the internal observer object.</param>
        /// <param name="innerObserverObjectField">The field of the internal observer object.</param>
        private void AddDispose(TypeDefinition decoratedType, TypeReference observerObjectType, FieldDefinition innerObserverObjectField)
        {
            var module = this.parentWeaver.ModuleDefinition;

            var disposeMethodDefinition = decoratedType.Methods.Where(x => x.Name == "Dispose").OrderBy(x => x.Parameters.Count).FirstOrDefault();

            bool isNew = false;

            // add Dispose method.
            if (disposeMethodDefinition == null)
            {
                disposeMethodDefinition = new MethodDefinition("Dispose", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, module.TypeSystem.Void);
                decoratedType.Methods.Add(disposeMethodDefinition);
                isNew = true;
            }

            var disposeReference = module.ImportReference(observerObjectType.Resolve().Methods.Single(x => x.Name == "Dispose"));

            var processor = disposeMethodDefinition.Body.GetILProcessor();

            if (isNew)
            {
                processor.Emit(OpCodes.Ret);
            }

            var originalStart = processor.Body.Instructions.First();

            var instructions = new List<Instruction>
            {
                processor.Create(OpCodes.Ldarg_0),
                processor.Create(OpCodes.Ldfld, innerObserverObjectField),
                processor.Create(OpCodes.Brfalse_S, originalStart),
                processor.Create(OpCodes.Ldarg_0),
                processor.Create(OpCodes.Ldfld, innerObserverObjectField),
                processor.Create(OpCodes.Callvirt, disposeReference),
            };

            foreach (var instruction in instructions)
            {
                processor.InsertBefore(originalStart, instruction);
            }
        }

        /// <summary>
        /// Emits IL code for assignment of the inner observer object field.
        /// </summary>
        /// <param name="processor">The processor to use.</param>
        /// <param name="observerName">The name of the observer.</param>
        /// <param name="innerObserverObjectField">The inner observer object field to assign.</param>
        /// <param name="buildRenderTreeMethod">The buildRenderTreeMethod on the compoent.</param>
        /// <param name="stateChangedMethod">The method to call to set the state to changed.</param>
        private void EmitObserverObjectInit(ILProcessor processor, string observerName, FieldDefinition innerObserverObjectField, MethodDefinition buildRenderTreeMethod, MethodDefinition stateChangedMethod)
        {
            var module = this.parentWeaver.ModuleDefinition;

            var observableObjectConstructor = module.ImportReference(this.parentWeaver.ModuleDefinition.ImportReference(this.weavingContext.CortexNetBlazorObserverObject).Resolve().Methods.Single(x => x.IsConstructor && !x.IsStatic));
            var buildRenderTreeReference = module.ImportReference(buildRenderTreeMethod);
            var stateChangedReference = module.ImportReference(stateChangedMethod);

            var renderActionType = buildRenderTreeMethod.GetActionType(this.weavingContext);

            MethodReference renderActionConstructorType = renderActionType.Resolve().Methods.Single(x => x.IsConstructor && !x.IsStatic);
            var renderActionConstructorReference = module.ImportReference(renderActionConstructorType.GetGenericMethodOnInstantance(renderActionType));

            var stateChangedActionType = stateChangedMethod.GetActionType(this.weavingContext);

            MethodReference stateChangedActionConstructorType = stateChangedActionType.Resolve().Methods.Single(x => x.IsConstructor && !x.IsStatic);
            var stateChangedActionConstructorReference = module.ImportReference(stateChangedActionConstructorType);

            var instructions = new List<Instruction>
            {
                // this.observerObject = new ObserverObject(sharedState, name, (Action<RenderTreeBuilder>)buildRenderTreeAction, (Action)stateChangedAction);
                processor.Create(OpCodes.Ldarg_0),
                processor.Create(OpCodes.Ldarg_1),
                processor.Create(OpCodes.Ldstr, observerName),
                processor.Create(OpCodes.Ldarg_0),
                processor.Create(OpCodes.Dup),
                processor.Create(OpCodes.Ldvirtftn, buildRenderTreeReference),
                processor.Create(OpCodes.Newobj, renderActionConstructorReference),
                processor.Create(OpCodes.Ldarg_0),
                processor.Create(OpCodes.Ldftn, stateChangedReference),
                processor.Create(OpCodes.Newobj, stateChangedActionConstructorReference),
                processor.Create(OpCodes.Newobj, observableObjectConstructor),
                processor.Create(OpCodes.Stfld, innerObserverObjectField),
            };

            foreach (var instruction in instructions)
            {
                processor.Append(instruction);
            }
        }

        /// <summary>
        /// Weaves the BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder) Method.
        /// </summary>
        /// <param name="buildRenderTreeMethod">The method definition of the method to weave.</param>
        /// <param name="observerObjectType">The type of the inner observer object.</param>
        /// <param name="observerObjectDefinition">The field definition of the observer object.</param>
        /// <param name="counterFieldDefinition">The field definition of the entrance counter field.</param>
        private void WeaveBuildRenderTreeMethod(MethodDefinition buildRenderTreeMethod, TypeReference observerObjectType, FieldDefinition observerObjectDefinition, FieldDefinition counterFieldDefinition)
        {
            var module = this.parentWeaver.ModuleDefinition;

            var renderFragmentReference = this.weavingContext.MicrosoftAspNetCoreComponentsRenderFragment;
            var renderFragmentConstructor = module.ImportReference(renderFragmentReference.Resolve().Methods.Single(x => x.IsConstructor));
            var reactiveRenderFragmentRefrence = module.ImportReference(observerObjectType.Resolve().Methods.Single(x => x.Name == "ReactiveRenderFragment" && !x.HasGenericParameters));

            var processor = buildRenderTreeMethod.Body.GetILProcessor();

            var targets = new List<Instruction>();

            foreach (var instruction in buildRenderTreeMethod.Body.Instructions)
            {
                // not a suitable render fragment.
                if (instruction.OpCode != OpCodes.Newobj ||
                    !(instruction.Operand is MethodReference mr) ||
                    mr.DeclaringType.FullName != renderFragmentConstructor.DeclaringType.FullName)
                {
                    continue;
                }

                targets.Add(instruction);
            }

            foreach (var target in targets)
            {
                processor.InsertBefore(target.Previous.Previous, processor.Create(OpCodes.Ldarg_0));
                processor.InsertBefore(target.Previous.Previous, processor.Create(OpCodes.Ldfld, module.ImportReference(observerObjectDefinition)));
                processor.InsertAfter(target, processor.Create(OpCodes.Call, reactiveRenderFragmentRefrence));
            }

            var genericTargets = new Dictionary<Instruction, GenericInstanceType>();
            var renderFragmentType = this.weavingContext.MicrosoftAspNetCoreComponentsGenericRenderFragment;
            var genericReactiveRenderFragmentMethodType = observerObjectType.Resolve().Methods.Single(x => x.Name == "ReactiveRenderFragment" && x.HasGenericParameters);

            foreach (var instruction in buildRenderTreeMethod.Body.Instructions)
            {
                // not a suitable render fragment.
                if (instruction.OpCode != OpCodes.Newobj ||
                    !(instruction.Operand is MethodReference mr) ||
                    !mr.DeclaringType.IsGenericInstance ||
                    mr.DeclaringType.Resolve().FullName != renderFragmentType.FullName)
                {
                    continue;
                }

                var genericInstance = mr.DeclaringType as GenericInstanceType;
                genericTargets.Add(instruction, genericInstance);
            }

            foreach (var target in genericTargets)
            {
                var instruction = target.Key;
                var genericInstance = target.Value;

                var reactiveRenderFragmentInstanceMethod = new GenericInstanceMethod(genericReactiveRenderFragmentMethodType);
                foreach (var parameter in genericInstance.GenericArguments)
                {
                    reactiveRenderFragmentInstanceMethod.GenericArguments.Add(parameter.Resolve());
                }

                processor.InsertBefore(instruction.Previous.Previous, processor.Create(OpCodes.Ldarg_0));
                processor.InsertBefore(instruction.Previous.Previous, processor.Create(OpCodes.Ldfld, module.ImportReference(observerObjectDefinition)));
                processor.InsertAfter(instruction, processor.Create(OpCodes.Call, module.ImportReference(reactiveRenderFragmentInstanceMethod)));
            }

            var originalStart = buildRenderTreeMethod.Body.Instructions.First();
            var originalEnd = buildRenderTreeMethod.Body.Instructions.Last();

            var prefix = new List<Instruction>
            {
                // if fieldDefinition == null, jump to originalStart.
                processor.Create(OpCodes.Ldarg_0),
                processor.Create(OpCodes.Ldfld, observerObjectDefinition),
                processor.Create(OpCodes.Brfalse_S, originalStart),

                // this pointers for later store and refetch. This bypasses local variable declarations that may not play nice with existing local variables.
                processor.Create(OpCodes.Ldarg_0),
                processor.Create(OpCodes.Ldarg_0),

                // load counterfield definition and add 1
                processor.Create(OpCodes.Ldarg_0),
                processor.Create(OpCodes.Ldfld, counterFieldDefinition),
                processor.Create(OpCodes.Ldc_I4_1),
                processor.Create(OpCodes.Add),

                // store and refetch. Divide by 2 and keep remainder.
                processor.Create(OpCodes.Stfld, counterFieldDefinition),
                processor.Create(OpCodes.Ldfld, counterFieldDefinition),
                processor.Create(OpCodes.Ldc_I4_2),
                processor.Create(OpCodes.Rem),

                // if remainder is not 1, jump to original start of function.
                processor.Create(OpCodes.Ldc_I4_1),
                processor.Create(OpCodes.Bne_Un_S, originalStart),

                // load the field where the action delegate is stored.
                processor.Create(OpCodes.Ldarg_0),
                processor.Create(OpCodes.Ldfld, observerObjectDefinition),
            };

            var invokeMethod = observerObjectType.Resolve().Methods.Single(x => x.Name == "BuildRenderTree");
            var invokeReference = module.ImportReference(invokeMethod);

            // push all function arguments onto the evaluation stack.
            for (int i = 0; i < buildRenderTreeMethod.Parameters.Count; i++)
            {
                prefix.Add(processor.Ldarg(i + 1));
            }

            // call the action delegate with the arguments on the evaluation stack.
            prefix.Add(processor.Create(OpCodes.Callvirt, invokeReference));

            // this pointers for fetch and store;
            prefix.Add(processor.Create(OpCodes.Ldarg_0));
            prefix.Add(processor.Create(OpCodes.Ldarg_0));

            // this.counterFieldDefinition -= 2;
            prefix.Add(processor.Create(OpCodes.Ldfld, counterFieldDefinition));
            prefix.Add(processor.Create(OpCodes.Ldc_I4_2));
            prefix.Add(processor.Create(OpCodes.Sub));
            prefix.Add(processor.Create(OpCodes.Stfld, counterFieldDefinition));

            prefix.Add(processor.Create(OpCodes.Ret));

            foreach (var instruction in prefix)
            {
                processor.InsertBefore(originalStart, instruction);
            }
        }
    }
}
