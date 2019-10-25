﻿// <copyright file="IDependencyNodeTests.cs" company="Michel Weststrate, Jan-Willem Spuij">
// Copyright 2019 Michel Weststrate, Jan-Willem Spuij
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

namespace Cortex.Net.Test
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Cortex.Net.Core;
    using Xunit;

    /// <summary>
    /// Unit tests for Implementations of <see cref="IDependencyNode"/>.
    /// </summary>
    public class IDependencyNodeTests
    {
        private static readonly SharedState SharedState = new SharedState(new CortexConfiguration());

        /// <summary>
        /// Gets the implementations of IDependencyNode.
        /// </summary>
        public static IEnumerable<object[]> Implementations
        {
            get
            {
                return new object[][]
                {
                    new object[] { new Atom(SharedState, "test") },
                    new object[] { new ComputedValue<int>(SharedState, new ComputedValueOptions<int>(() => 3, "test")) },
                    new object[] { new Reaction(SharedState, "test", () => { }) },
                };
            }
        }

        /// <summary>
        /// Tests whether the name of the Node is not null.
        /// </summary>
        /// <param name="node">The node to test.</param>
        [Theory]
        [MemberData(nameof(Implementations))]
        public void NameNotNull(IDependencyNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            Assert.NotNull(node.Name);
        }

        /// <summary>
        /// Tests whether the Shared State is not null and equals central SharedState.
        /// </summary>
        /// <param name="node">The node to test.</param>
        [Theory]
        [MemberData(nameof(Implementations))]
        public void SharedStateTest(IDependencyNode node)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            Assert.NotNull(node.SharedState);
            Assert.Equal(node.SharedState, SharedState);
        }
    }
}
