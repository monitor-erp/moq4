// Copyright (c) 2007, Clarius Consulting, Manas Technology Solutions, InSTEDD, and Contributors.
// All rights reserved. Licensed under the BSD 3-Clause License; see License.txt.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Moq
{
	/// <summary>
	/// Provides partial evaluation of subtrees, whenever they can be evaluated locally.
	/// </summary>
	/// <author>Matt Warren: http://blogs.msdn.com/mattwar</author>
	/// <contributor>Documented by InSTEDD: http://www.instedd.org</contributor>
	internal static class Evaluator
	{
		/// <summary>
		/// Performs evaluation and replacement of independent sub-trees
		/// </summary>
		/// <param name="expression">The root of the expression tree.</param>
		/// <param name="fnCanBeEvaluated">A function that decides whether a given expression
		/// node can be part of the local function.</param>
		/// <returns>A new tree with sub-trees evaluated and replaced.</returns>
		public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
		{
			return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);
		}

		/// <summary>
		/// Performs evaluation and replacement of independent sub-trees
		/// </summary>
		/// <param name="expression">The root of the expression tree.</param>
		/// <returns>A new tree with sub-trees evaluated and replaced.</returns>
		public static Expression PartialEval(Expression expression)
		{
			return PartialEval(expression, e => e.NodeType != ExpressionType.Parameter && !(e is MatchExpression));
		}

		/// <summary>
		/// Evaluates and replaces sub-trees when first candidate is reached (top-down)
		/// </summary>
		private class SubtreeEvaluator : ExpressionVisitor
		{
			private HashSet<Expression> candidates;

			internal SubtreeEvaluator(HashSet<Expression> candidates)
			{
				this.candidates = candidates;
			}

			internal Expression Eval(Expression exp)
			{
				return this.Visit(exp);
			}

			public override Expression Visit(Expression exp)
			{
				if (exp == null)
				{
					return null;
				}
				if (this.candidates.Contains(exp))
				{
					return Evaluate(exp);
				}
				return base.Visit(exp);
			}

			private static Expression Evaluate(Expression e)
			{
				if (e.NodeType == ExpressionType.Constant)
				{
					return e;
				}
				LambdaExpression lambda = Expression.Lambda(e);
				Delegate fn = lambda.CompileUsingExpressionCompiler();
				return Expression.Constant(fn.DynamicInvoke(null), e.Type);
			}
		}

		/// <summary>
		/// Performs bottom-up analysis to determine which nodes can possibly
		/// be part of an evaluated sub-tree.
		/// </summary>
		private class Nominator : ExpressionVisitor
		{
			private Func<Expression, bool> fnCanBeEvaluated;
			private HashSet<Expression> candidates;
			private bool cannotBeEvaluated;

			internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
			{
				this.fnCanBeEvaluated = fnCanBeEvaluated;
			}

			internal HashSet<Expression> Nominate(Expression expression)
			{
				this.candidates = new HashSet<Expression>();
				this.Visit(expression);
				return this.candidates;
			}

			public override Expression Visit(Expression expression)
			{
				if (expression != null)
				{
					bool saveCannotBeEvaluated = this.cannotBeEvaluated;
					this.cannotBeEvaluated = false;
					base.Visit(expression);
					if (!this.cannotBeEvaluated)
					{
						bool canBeEvaluated;
						try
						{
							canBeEvaluated = this.fnCanBeEvaluated(expression);
						}
						catch
						{
							canBeEvaluated = false;
						}

						if (canBeEvaluated)
						{
							// A byref-like value (e.g. the `ReadOnlySpan<T>` that C# 14 first-class span conversions
							// introduce for `array.Contains(x)`) cannot be boxed into a constant, so it must stay in
							// the tree. Its operands, and a parent returning an ordinary type, can still be evaluated.
							if (!IsByRefLike(expression.Type))
							{
								this.candidates.Add(expression);
							}
						}
						else
						{
							this.cannotBeEvaluated = true;
						}
					}

					this.cannotBeEvaluated |= saveCannotBeEvaluated;
				}

				return expression;
			}

			private static bool IsByRefLike(Type type)
			{
				// `Type.IsByRefLike` is unavailable on net45 / netstandard2.0, so look for the attribute the compiler emits.
				foreach (var attribute in type.GetCustomAttributes(inherit: false))
				{
					if (attribute.GetType().FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute")
					{
						return true;
					}
				}

				return false;
			}
		}
	}
}
