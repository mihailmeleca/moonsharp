﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Grammar;

namespace MoonSharp.Interpreter.Tree.Expressions
{
	class TableConstructor : Expression 
	{
		List<Expression> m_PositionalValues = new List<Expression>();
		List<KeyValuePair<Expression, Expression>> m_CtorArgs = new List<KeyValuePair<Expression, Expression>>();

		public TableConstructor(LuaParser.TableconstructorContext context, ScriptLoadingContext lcontext)
			: base(context, lcontext)
		{
			var fieldlist = context.fieldlist();

			if (fieldlist != null)
			{
				foreach (var field in fieldlist.field())
				{
					var keyval = field.keyexp;
					var name = field.NAME();

					if (keyval != null)
					{
						Expression exp = NodeFactory.CreateExpression(keyval, lcontext);

						m_CtorArgs.Add(new KeyValuePair<Expression,Expression>(
							exp,
							NodeFactory.CreateExpression(field.keyedexp, lcontext)));
					}
					else if (name != null)
					{
						m_CtorArgs.Add(new KeyValuePair<Expression, Expression>(
							new LiteralExpression(field, lcontext, new RValue(name.GetText())),
							NodeFactory.CreateExpression(field.namedexp, lcontext)));
					}
					else 
					{
						m_PositionalValues.Add(NodeFactory.CreateExpression(field.positionalexp, lcontext));
					}
				}

			}
		}


		public override RValue Eval(RuntimeScope scope)
		{
			var dic = m_CtorArgs.ToDictionary(
				kvp => kvp.Key.Eval(scope),
				kvp => kvp.Value.Eval(scope).ToSimplestValue().CloneAsWritable());

			Table t = new Table(dic, m_PositionalValues.Select(e => e.Eval(scope)));

			return new RValue(t);
		}

		public override void Compile(Execution.VM.Chunk bc)
		{
			bc.NewTable();

			foreach (var kvp in m_CtorArgs)
			{
				kvp.Key.Compile(bc);
				bc.IndexRefN();
				kvp.Value.Compile(bc);
				bc.Store();
			}

			for (int i = 0; i < m_PositionalValues.Count; i++)
			{
				bc.Literal(new RValue(i+1));
				bc.IndexRefN();
				m_PositionalValues[i].Compile(bc);
				bc.Store();
			}
		}

	}
}
