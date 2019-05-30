﻿using System;
using NCalc.Domain;
using System.Collections.Generic;
using System.Threading;
using System.Collections;

using UnityEngine;

using NUnit.Framework;

namespace NCalc.Tests
{
	public class Fixtures
	{
		[Test]
		public void ExpressionShouldEvaluate()
		{
			var expressions = new []
			{
				"2 + 3 + 5",
				"2 * 3 + 5",
				"2 * (3 + 5)",
				"2 * (2*(2*(2+1)))",
				"10 % 3",
				"true or false",
				"not true",
				"false || not (false and true)",
				"3 > 2 and 1 <= (3-2)",
				"3 % 2 != 10 % 3"
			};

			foreach(string expression in expressions)
				Debug.LogFormat("{0} = {1}",expression,new Expression(expression).Evaluate());
		}

		[Test]
		public void ShouldParseValues()
		{
			Assert.AreEqual(123456,new Expression("123456").Evaluate());
			Assert.AreEqual(new DateTime(2001,01,01),new Expression("#01/01/2001#").Evaluate());
			Assert.AreEqual(123.456d,new Expression("123.456").Evaluate());
			Assert.AreEqual(true,new Expression("true").Evaluate());
			Assert.AreEqual("true",new Expression("'true'").Evaluate());
			Assert.AreEqual("azerty",new Expression("'azerty'").Evaluate());
		}

		[Test]
		public void ParsedExpressionToStringShouldHandleSmallDecimals()
		{
			// small decimals starting with 0 resulting in scientific notation did not work in original NCalc
			var equation = "0.000001";
			var testExpression = new Expression(equation);
			testExpression.Evaluate();
			Assert.AreEqual(equation,testExpression.ParsedExpression.ToString());
		}

		[Test]
		public void ShouldHandleUnicode()
		{
			Assert.AreEqual("経済協力開発機構",new Expression("'経済協力開発機構'").Evaluate());
			Assert.AreEqual("Hello",new Expression(@"'\u0048\u0065\u006C\u006C\u006F'").Evaluate());
			Assert.AreEqual("だ",new Expression(@"'\u3060'").Evaluate());
			Assert.AreEqual("\u0100",new Expression(@"'\u0100'").Evaluate());
		}

		[Test]
		public void ShouldEscapeCharacters()
		{
			Assert.AreEqual("'hello'",new Expression(@"'\'hello\''").Evaluate());
			Assert.AreEqual(" ' hel lo ' ",new Expression(@"' \' hel lo \' '").Evaluate());
			Assert.AreEqual("hel\nlo",new Expression(@"'hel\nlo'").Evaluate());
		}

		[Test]
		public void ShouldDisplayErrorMessages()
		{
			try
			{
				new Expression("(3 + 2").Evaluate();
				throw new Exception();
			}
			catch(EvaluationException e)
			{
				Debug.Log("Error catched: " + e.Message);
			}
		}

		[Test]
		public void Maths()
		{
			Assert.AreEqual(1M,new Expression("Abs(-1)").Evaluate());
			Assert.AreEqual(0d,new Expression("Acos(1)").Evaluate());
			Assert.AreEqual(0d,new Expression("Asin(0)").Evaluate());
			Assert.AreEqual(0d,new Expression("Atan(0)").Evaluate());
			Assert.AreEqual(2d,new Expression("Ceiling(1.5)").Evaluate());
			Assert.AreEqual(1d,new Expression("Cos(0)").Evaluate());
			Assert.AreEqual(1d,new Expression("Exp(0)").Evaluate());
			Assert.AreEqual(1d,new Expression("Floor(1.5)").Evaluate());
			Assert.AreEqual(-1d,new Expression("IEEERemainder(3,2)").Evaluate());
			Assert.AreEqual(0d,new Expression("Log(1,10)").Evaluate());
			Assert.AreEqual(0d,new Expression("Log10(1)").Evaluate());
			Assert.AreEqual(9d,new Expression("Pow(3,2)").Evaluate());
			Assert.AreEqual(3.22d,new Expression("Round(3.222,2)").Evaluate());
			Assert.AreEqual(-1,new Expression("Sign(-10)").Evaluate());
			Assert.AreEqual(0d,new Expression("Sin(0)").Evaluate());
			Assert.AreEqual(2d,new Expression("Sqrt(4)").Evaluate());
			Assert.AreEqual(0d,new Expression("Tan(0)").Evaluate());
			Assert.AreEqual(1d,new Expression("Truncate(1.7)").Evaluate());
		}

		[Test]
		public void ExpressionShouldEvaluateCustomFunctions()
		{
			var e = new Expression("SecretOperation(3, 6)");

			e.EvaluateFunction += delegate (string name,FunctionArgs args) {
				if(name == "SecretOperation")
					args.Result = (int)args.Parameters[0].Evaluate() + (int)args.Parameters[1].Evaluate();
			};

			Assert.AreEqual(9,e.Evaluate());
		}

		[Test]
		public void ExpressionShouldEvaluateCustomFunctionsWithParameters()
		{
			var e = new Expression("SecretOperation([e], 6) + f");
			e.Parameters["e"] = 3;
			e.Parameters["f"] = 1;

			e.EvaluateFunction += delegate (string name,FunctionArgs args) {
				if(name == "SecretOperation")
					args.Result = (int)args.Parameters[0].Evaluate() + (int)args.Parameters[1].Evaluate();
			};

			Assert.AreEqual(10,e.Evaluate());
		}

		[Test]
		public void ExpressionShouldEvaluateParameters()
		{
			var e = new Expression("Round(Pow(Pi, 2) + Pow([Pi Squared], 2) + [X], 2)");

			e.Parameters["Pi Squared"] = new Expression("Pi * [Pi]");
			e.Parameters["X"] = 10;

			e.EvaluateParameter += delegate (string name,ParameterArgs args) {
				if(name == "Pi")
					args.Result = 3.14;
			};

			Assert.AreEqual(117.07,e.Evaluate());
		}

		[Test]
		public void ShouldEvaluateConditionnal()
		{
			var eif = new Expression("if([divider] <> 0, [divided] / [divider], 0)");
			eif.Parameters["divider"] = 5;
			eif.Parameters["divided"] = 5;

			Assert.AreEqual(1d,eif.Evaluate());

			eif = new Expression("if([divider] <> 0, [divided] / [divider], 0)");
			eif.Parameters["divider"] = 0;
			eif.Parameters["divided"] = 5;
			Assert.AreEqual(0,eif.Evaluate());
		}

		[Test]
		public void ShouldOverrideExistingFunctions()
		{
			var e = new Expression("Round(1.99, 2)");

			Assert.AreEqual(1.99d,e.Evaluate());

			e.EvaluateFunction += delegate (string name,FunctionArgs args) {
				if(name == "Round")
					args.Result = 3;
			};

			Assert.AreEqual(3,e.Evaluate());
		}

		[Test]
		public void ShouldEvaluateInOperator()
		{
			// The last argument should not be evaluated
			var ein = new Expression("in((2 + 2), [1], [2], 1 + 2, 4, 1 / 0)");
			ein.Parameters["1"] = 2;
			ein.Parameters["2"] = 5;

			Assert.AreEqual(true,ein.Evaluate());

			var eout = new Expression("in((2 + 2), [1], [2], 1 + 2, 3)");
			eout.Parameters["1"] = 2;
			eout.Parameters["2"] = 5;

			Assert.AreEqual(false,eout.Evaluate());

			// Should work with strings
			var estring = new Expression("in('to' + 'to', 'titi', 'toto')");

			Assert.AreEqual(true,estring.Evaluate());

		}

		[TestCase("!true",false)]
		[TestCase("not false",true)]
		[TestCase("2 * 3",6)]
		[TestCase("6 / 2",3d)]
		[TestCase("7 % 2",1)]
		[TestCase("2 + 3",5)]
		[TestCase("2 - 1",1)]
		[TestCase("1 < 2",true)]
		[TestCase("1 > 2",false)]
		[TestCase("1 <= 2",true)]
		[TestCase("1 <= 1",true)]
		[TestCase("1 >= 2",false)]
		[TestCase("1 >= 1",true)]
		[TestCase("1 = 1",true)]
		[TestCase("1 == 1",true)]
		[TestCase("1 != 1",false)]
		[TestCase("1 <> 1",false)]
		[TestCase("1 & 1",1)]
		[TestCase("1 | 1",1)]
		[TestCase("1 ^ 1",0)]
		[TestCase("~1",~1)]
		[TestCase("2 >> 1",1)]
		[TestCase("2 << 1",4)]
		[TestCase("true && false",false)]
		[TestCase("true and false",false)]
		[TestCase("true || false",true)]
		[TestCase("true or false",true)]
		[TestCase("if(true, 0, 1)",0)]
		[TestCase("if(false, 0, 1)",1)]
		public void ShouldEvaluateOperators(string exp,object expected)
		{
			Assert.AreEqual(expected,new Expression(exp).Evaluate());
		}

		[Test]
		public void ShouldHandleOperatorsPriority()
		{
			Assert.AreEqual(8,new Expression("2+2+2+2").Evaluate());
			Assert.AreEqual(16,new Expression("2*2*2*2").Evaluate());
			Assert.AreEqual(6,new Expression("2*2+2").Evaluate());
			Assert.AreEqual(6,new Expression("2+2*2").Evaluate());

			Assert.AreEqual(9d,new Expression("1 + 2 + 3 * 4 / 2").Evaluate());
			Assert.AreEqual(13.5,new Expression("18/2/2*3").Evaluate());
		}

		[Test]
		public void ShouldNotLoosePrecision()
		{
			Assert.AreEqual(0.5,new Expression("3/6").Evaluate());
		}

		[Test]
		public void ShouldThrowAnExpcetionWhenInvalidNumber()
		{
			Assert.Throws<EvaluationException>(() => new Expression("4. + 2").Evaluate());
		}

		[Test]
		public void ShouldNotRoundDecimalValues()
		{
			Assert.AreEqual(false,new Expression("0 <= -0.6").Evaluate());
		}

		[Test]
		public void ShouldEvaluateTernaryExpression()
		{
			Assert.AreEqual(1,new Expression("1+2<3 ? 3+4 : 1").Evaluate());
		}

		[Test]
		public void ShouldSerializeExpression()
		{
			Assert.AreEqual("True and False",new BinaryExpression(BinaryExpressionType.And,new ValueExpression(true),new ValueExpression(false)).ToString());
			Assert.AreEqual("1 / 2",new BinaryExpression(BinaryExpressionType.Div,new ValueExpression(1),new ValueExpression(2)).ToString());
			Assert.AreEqual("1 = 2",new BinaryExpression(BinaryExpressionType.Equal,new ValueExpression(1),new ValueExpression(2)).ToString());
			Assert.AreEqual("1 > 2",new BinaryExpression(BinaryExpressionType.Greater,new ValueExpression(1),new ValueExpression(2)).ToString());
			Assert.AreEqual("1 >= 2",new BinaryExpression(BinaryExpressionType.GreaterOrEqual,new ValueExpression(1),new ValueExpression(2)).ToString());
			Assert.AreEqual("1 < 2",new BinaryExpression(BinaryExpressionType.Lesser,new ValueExpression(1),new ValueExpression(2)).ToString());
			Assert.AreEqual("1 <= 2",new BinaryExpression(BinaryExpressionType.LesserOrEqual,new ValueExpression(1),new ValueExpression(2)).ToString());
			Assert.AreEqual("1 - 2",new BinaryExpression(BinaryExpressionType.Minus,new ValueExpression(1),new ValueExpression(2)).ToString());
			Assert.AreEqual("1 % 2",new BinaryExpression(BinaryExpressionType.Modulo,new ValueExpression(1),new ValueExpression(2)).ToString());
			Assert.AreEqual("1 != 2",new BinaryExpression(BinaryExpressionType.NotEqual,new ValueExpression(1),new ValueExpression(2)).ToString());
			Assert.AreEqual("True or False",new BinaryExpression(BinaryExpressionType.Or,new ValueExpression(true),new ValueExpression(false)).ToString());
			Assert.AreEqual("1 + 2",new BinaryExpression(BinaryExpressionType.Plus,new ValueExpression(1),new ValueExpression(2)).ToString());
			Assert.AreEqual("1 * 2",new BinaryExpression(BinaryExpressionType.Times,new ValueExpression(1),new ValueExpression(2)).ToString());

			Assert.AreEqual("-(True and False)",new UnaryExpression(UnaryExpressionType.Negate,new BinaryExpression(BinaryExpressionType.And,new ValueExpression(true),new ValueExpression(false))).ToString());
			Assert.AreEqual("!(True and False)",new UnaryExpression(UnaryExpressionType.Not,new BinaryExpression(BinaryExpressionType.And,new ValueExpression(true),new ValueExpression(false))).ToString());

			Assert.AreEqual("test(True and False, -(True and False))",new Function(new Identifier("test"),new LogicalExpression[] { new BinaryExpression(BinaryExpressionType.And,new ValueExpression(true),new ValueExpression(false)),new UnaryExpression(UnaryExpressionType.Negate,new BinaryExpression(BinaryExpressionType.And,new ValueExpression(true),new ValueExpression(false))) }).ToString());

			Assert.AreEqual("True",new ValueExpression(true).ToString());
			Assert.AreEqual("False",new ValueExpression(false).ToString());
			Assert.AreEqual("1",new ValueExpression(1).ToString());
			Assert.AreEqual("1.234",new ValueExpression(1.234).ToString());
			Assert.AreEqual("'hello'",new ValueExpression("hello").ToString());
			Assert.AreEqual("#" + new DateTime(2009,1,1) + "#",new ValueExpression(new DateTime(2009,1,1)).ToString());

			Assert.AreEqual("Sum(1 + 2)",new Function(new Identifier("Sum"),new[] { new BinaryExpression(BinaryExpressionType.Plus,new ValueExpression(1),new ValueExpression(2)) }).ToString());
		}

		[Test]
		public void ShouldHandleStringConcatenation()
		{
			Assert.AreEqual("toto",new Expression("'to' + 'to'").Evaluate());
			Assert.AreEqual("one2",new Expression("'one' + 2").Evaluate());
			Assert.AreEqual(3M,new Expression("1 + '2'").Evaluate());
		}

		[Test]
		public void ShouldDetectSyntaxErrorsBeforeEvaluation()
		{
			var e = new Expression("a + b * (");
			Assert.Null(e.Error);
			Assert.True(e.HasErrors());
			Assert.True(e.HasErrors());
			Assert.NotNull(e.Error);

			e = new Expression("+ b ");
			Assert.Null(e.Error);
			Assert.True(e.HasErrors());
			Assert.NotNull(e.Error);
		}

		[Test]
		public void ShouldReuseCompiledExpressionsInMultiThreadedMode()
		{
			// Repeats the tests n times
			for(int cpt = 0; cpt < 20; cpt++)
			{
				const int nbthreads = 30;
				_exceptions = new List<Exception>();
				var threads = new Thread[nbthreads];

				// Starts threads
				for(int i = 0; i < nbthreads; i++)
				{
					var thread = new Thread(WorkerThread);
					thread.Start();
					threads[i] = thread;
				}

				// Waits for end of threads
				bool running = true;
				while(running)
				{
					Thread.Sleep(100);
					running = false;
					for(int i = 0; i < nbthreads; i++)
					{
						if(threads[i].ThreadState == ThreadState.Running)
							running = true;
					}
				}

				if(_exceptions.Count > 0)
				{
					Debug.Log(_exceptions[0].StackTrace);
					throw _exceptions[0];
				}
			}
		}

		private List<Exception> _exceptions;

		private void WorkerThread()
		{
			try
			{
				var r1 = new System.Random((int)DateTime.Now.Ticks);
				var r2 = new System.Random((int)DateTime.Now.Ticks);
				int n1 = r1.Next(10);
				int n2 = r2.Next(10);

				// Constructs a simple addition randomly. Odds are that the same expression gets constructed multiple times by different threads
				var exp = n1 + " + " + n2;
				var e = new Expression(exp);
				Assert.True(e.Evaluate().Equals(n1 + n2));
			}
			catch(Exception e)
			{
				_exceptions.Add(e);
			}
		}

		[Test]
		public void ShouldHandleCaseSensitiveness()
		{
			Assert.AreEqual(1M,new Expression("aBs(-1)",EvaluateOptions.IgnoreCase).Evaluate());
			Assert.AreEqual(1M,new Expression("Abs(-1)",EvaluateOptions.None).Evaluate());

			Assert.Throws<ArgumentException>(() => Assert.AreEqual(1M,new Expression("aBs(-1)",EvaluateOptions.None).Evaluate()));
		}

		[Test]
		public void ShouldHandleCustomParametersWhenNoSpecificParameterIsDefined()
		{
			var e = new Expression("Round(Pow([Pi], 2) + Pow([Pi], 2) + 10, 2)");

			e.EvaluateParameter += delegate (string name,ParameterArgs arg) {
				if(name == "Pi")
					arg.Result = 3.14;
			};

			e.Evaluate();
		}

		[Test]
		public void ShouldHandleCustomFunctionsInFunctions()
		{
			var e = new Expression("if(true, func1(x) + func2(func3(y)), 0)");

			e.EvaluateFunction += delegate (string name,FunctionArgs arg) {
				switch(name)
				{
					case "func1":
						arg.Result = 1;
						break;
					case "func2":
						arg.Result = 2 * Convert.ToDouble(arg.Parameters[0].Evaluate());
						break;
					case "func3":
						arg.Result = 3 * Convert.ToDouble(arg.Parameters[0].Evaluate());
						break;
				}
			};

			e.EvaluateParameter += delegate (string name,ParameterArgs arg) {
				switch(name)
				{
					case "x":
						arg.Result = 1;
						break;
					case "y":
						arg.Result = 2;
						break;
					case "z":
						arg.Result = 3;
						break;
				}
			};

			Assert.AreEqual(13d,e.Evaluate());
		}


		[Test]
		public void ShouldParseScientificNotation()
		{
			Assert.AreEqual(12.2d,new Expression("1.22e1").Evaluate());
			Assert.AreEqual(100d,new Expression("1e2").Evaluate());
			Assert.AreEqual(100d,new Expression("1e+2").Evaluate());
			Assert.AreEqual(0.01d,new Expression("1e-2").Evaluate());
			Assert.AreEqual(0.001d,new Expression(".1e-2").Evaluate());
			Assert.AreEqual(10000000000d,new Expression("1e10").Evaluate());
		}

		[Test]
		public void ShouldEvaluateArrayParameters()
		{
			var e = new Expression("x * x", EvaluateOptions.IterateParameters);
			e.Parameters["x"] = new[] { 0,1,2,3,4 };

			CollectionAssert.AreEqual(new[] { 0,1,4,9,16 },e.Evaluate() as IEnumerable);
		}

		[Test]
		public void CustomFunctionShouldReturnNull()
		{
			var e = new Expression("SecretOperation(3, 6)");

			e.EvaluateFunction += delegate (string name,FunctionArgs args) {
				Assert.False(args.HasResult);
				if(name == "SecretOperation")
					args.Result = null;
				Assert.True(args.HasResult);
			};

			Assert.AreEqual(null,e.Evaluate());
		}

		[Test]
		public void CustomParametersShouldReturnNull()
		{
			var e = new Expression("x");

			e.EvaluateParameter += delegate (string name,ParameterArgs args) {
				Assert.False(args.HasResult);
				if(name == "x")
					args.Result = null;
				Assert.True(args.HasResult);
			};

			Assert.AreEqual(null,e.Evaluate());
		}

		[Test]
		public void ShouldCompareDates()
		{
			Assert.AreEqual(true,new Expression("#1/1/2009#==#1/1/2009#").Evaluate());
			Assert.AreEqual(false,new Expression("#2/1/2009#==#1/1/2009#").Evaluate());
		}

		[Test]
		public void ShouldRoundAwayFromZero()
		{
			Assert.AreEqual(22d,new Expression("Round(22.5, 0)").Evaluate());
			Assert.AreEqual(23d,new Expression("Round(22.5, 0)",EvaluateOptions.RoundAwayFromZero).Evaluate());
		}

		[Test]
		public void ShouldEvaluateSubExpressions()
		{
			var volume = new Expression("[surface] * h");
			var surface = new Expression("[l] * [L]");
			volume.Parameters["surface"] = surface;
			volume.Parameters["h"] = 3;
			surface.Parameters["l"] = 1;
			surface.Parameters["L"] = 2;

			Assert.AreEqual(6,volume.Evaluate());
		}

		[Test]
		public void ShouldHandleLongValues()
		{
			Assert.AreEqual(40000000000 + 1f,new Expression("40000000000+1").Evaluate());
		}

		[Test]
		public void ShouldCompareLongValues()
		{
			Assert.AreEqual(false,new Expression("(0=1500000)||(((0+2200000000)-1500000)<0)").Evaluate());
		}

		[Test]
		public void ShouldDisplayErrorIfUncompatibleTypes()
		{
			var e = new Expression("(a > b) + 10");
			e.Parameters["a"] = 1;
			e.Parameters["b"] = 2;
			Assert.Throws<InvalidOperationException>(() => e.Evaluate());
		}

		[TestCase("(X1 = 1)/2",0.5)]
		[TestCase("(X1 = 1)*2",2)]
		[TestCase("(X1 = 1)+1",2)]
		[TestCase("(X1 = 1)-1",0)]
		[TestCase("2*(X1 = 1)",2)]
		[TestCase("2/(X1 = 1)",2.0)]
		[TestCase("1+(X1 = 1)",2)]
		[TestCase("1-(X1 = 1)",0)]
		public void ShouldOptionallyCalculateWithBoolean(string formula,object expectedValue)
		{
			var expression = new Expression(formula, EvaluateOptions.BooleanCalculation) {Parameters = {["X1"] = 1}};

			Assert.AreEqual(expression.Evaluate(),expectedValue);

			var lambda = expression.ToLambda<object>();
			Assert.AreEqual(lambda(),expectedValue);
		}

		[Test]
		public void ShouldNotConvertRealTypes()
		{
			var e = new Expression("x/2");
			e.Parameters["x"] = 2F;
			Assert.AreEqual(typeof(float),e.Evaluate().GetType());

			e = new Expression("x/2");
			e.Parameters["x"] = 2D;
			Assert.AreEqual(typeof(double),e.Evaluate().GetType());

			e = new Expression("x/2");
			e.Parameters["x"] = 2m;
			Assert.AreEqual(typeof(decimal),e.Evaluate().GetType());

			e = new Expression("a / b * 100");
			e.Parameters["a"] = 20M;
			e.Parameters["b"] = 20M;
			Assert.AreEqual(100M,e.Evaluate());

		}

		[Test]
		public void ShouldShortCircuitBooleanExpressions()
		{
			var e = new Expression("([a] != 0) && ([b]/[a]>2)");
			e.Parameters["a"] = 0;

			Assert.AreEqual(false,e.Evaluate());
		}

		[Test]
		public void ShouldAddDoubleAndDecimal()
		{
			var e = new Expression("1.8 + Abs([var1])");
			e.Parameters["var1"] = 9.2;

			Assert.AreEqual(11M,e.Evaluate());
		}

		[TestCase(-0.5,-0.5,0)]
		[TestCase(1.8,9.2,11)]
		public void ShouldAllowBracketParameter(object var0,object var1,object result)
		{
			var e = new Expression("[var[0]] + Abs([var[1]])");
			e.Parameters["var[0]"] = var0;
			e.Parameters["var[1]"] = var1;

			Assert.AreEqual(result,e.Evaluate());
		}

		[Test]
		public void ShouldAllowStringParameter()
		{
			var e = new Expression("[var[0]] + ' ' + [var[1]]");
			e.Parameters["var[0]"] = "TestTest";
			e.Parameters["var[1]"] = "0.001";
			Assert.AreEqual("TestTest 0.001",e.Evaluate());
		}
	}
}
