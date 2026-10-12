// Copyright (c) 2007, Clarius Consulting, Manas Technology Solutions, InSTEDD, and Contributors.
// All rights reserved. Licensed under the BSD 3-Clause License; see License.txt.

using System;
using System.Linq.Expressions;

using Xunit;

namespace Moq.Tests
{
	public class EvaluatorFixture
	{
		private static readonly long[] values = { 3, 4 };

		[Fact]
		public void PartialEval_does_not_box_byref_like_subtree()
		{
			var parameter = Expression.Parameter(typeof(long), "x");
			var expression = Expression.Lambda<Func<long, bool>>(ContainsCall(parameter), parameter);

			var evaluated = (Expression<Func<long, bool>>)expression.PartialEval();

			var compiled = evaluated.Compile();
			Assert.True(compiled(4));
			Assert.False(compiled(5));
		}

		[Fact]
		public void PartialEval_evaluates_parent_of_byref_like_subtree_when_it_has_an_ordinary_type()
		{
			var evaluated = ContainsCall(Expression.Constant(4L)).PartialEval();

			var constant = Assert.IsType<ConstantExpression>(evaluated);
			Assert.Equal(true, constant.Value);
		}

		[Fact]
		public void Setups_whose_matchers_contain_byref_like_subtrees_can_be_compared()
		{
			var mock = new Mock<IFoo>();
			mock.Setup(Setup(new long[] { 1, 2 })).Returns("A");
			mock.Setup(Setup(values)).Returns("B");

			Assert.Equal("A", mock.Object.Get(2));
			Assert.Equal("B", mock.Object.Get(4));
			Assert.Null(mock.Object.Get(9));
		}

		// Mirrors the tree C# 14 first-class span conversions produce for `array.Contains(x)`:
		// `Contains(op_Implicit(array), x)`, where the conversion returns a ref struct.
		private static Expression ContainsCall(Expression value, long[] array = null)
		{
			var arrayExpression = array == null
				? (Expression)Expression.Field(null, typeof(EvaluatorFixture).GetField(nameof(values), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
				: Expression.Constant(array);
			return Expression.Call(
				typeof(LongSpan).GetMethod(nameof(LongSpan.Contains)),
				Expression.Convert(arrayExpression, typeof(LongSpan), typeof(LongSpan).GetMethod("op_Implicit")),
				value);
		}

		private static Expression<Func<IFoo, string>> Setup(long[] array)
		{
			var argument = Expression.Parameter(typeof(long), "arg");
			var predicate = Expression.Lambda<Func<long, bool>>(ContainsCall(argument, array), argument);
			var itIs = Expression.Call(typeof(It), nameof(It.Is), new[] { typeof(long) }, Expression.Quote(predicate));
			var mock = Expression.Parameter(typeof(IFoo), "x");
			return Expression.Lambda<Func<IFoo, string>>(Expression.Call(mock, typeof(IFoo).GetMethod(nameof(IFoo.Get)), itIs), mock);
		}

		public interface IFoo
		{
			string Get(long value);
		}

		public ref struct LongSpan
		{
			private readonly long[] array;

			private LongSpan(long[] array)
			{
				this.array = array;
			}

			public static implicit operator LongSpan(long[] array) => new LongSpan(array);

			public static bool Contains(LongSpan span, long value) => Array.IndexOf(span.array, value) >= 0;
		}
	}
}
