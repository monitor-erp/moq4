// Copyright (c) 2007, Clarius Consulting, Manas Technology Solutions, InSTEDD, and Contributors.
// All rights reserved. Licensed under the BSD 3-Clause License; see License.txt.

using System;
using System.Collections.Generic;
using System.Reflection;
using Moq.Async;

namespace Moq
{
	/// <summary>
	/// Provides information about an invocation of a mock object.
	/// </summary>
	public interface IInvocation
	{
		/// <summary>
		/// Gets the method of the invocation.
		/// </summary>
		MethodInfo Method { get; }

		/// <summary>
		/// Gets the arguments of the invocation.
		/// </summary>
		object[] Arguments { get; }

		/// <summary>
		///   Gets the setup that matched this invocation (or <see langword="null"/> if there was no matching setup).
		/// </summary>
		ISetup MatchingSetup { get; }

		/// <summary>
		///   Gets whether this invocation was successfully verified by any of the various <c>`Verify`</c> methods.
		/// </summary>
		bool IsVerified { get; }

		/// <summary>
		/// The value being returned for a non-void method if no exception was thrown.
		/// </summary>
		object ReturnValue { get; set; }

		/// <summary>
		/// Optional exception if the method invocation results in an exception being thrown.
		/// </summary>
		Exception Exception { get; set; }

		///
		Type ProxyType { get; }

		///
		MethodInfo MethodImplementation { get; }

		/// 
		void MarkAsMatchedBy(ISetup setup);

		///
		void ConvertResultToAwaitable(IAwaitableFactory awaitableFactory);

		///
		object CallBase();
	}
}