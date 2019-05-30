using NUnit.Framework;

namespace NCalc.Tests
{
    public class Lambdas
    {
        private class Context
        {
            public int FieldA { get; set; }
            public string FieldB { get; set; }
            public decimal FieldC { get; set; }
            public decimal? FieldD { get; set; }
            public int? FieldE { get; set; }

            public int Test(int a, int b)
            {
                return a + b;
            }
            
            public string Test(string a, string b) 
            {
                return a + b;
            }

            public int Test(int a, int b, int c) 
            {
                return a + b + c;
            }

            public string Sum(string msg, params int[] numbers) {
                int total = 0;
                foreach (var num in numbers) {
                    total += num;
                }
                return msg + total;
            }

            public int Sum(params int[] numbers) 
            {
                int total = 0;
                foreach (var num in numbers) {
                    total += num;
                }
                return total;
            }

        }

        [TestCase("1+2", 3)]
        [TestCase("1-2", -1)]
        [TestCase("2*2", 4)]
        [TestCase("10/2", 5)]
        [TestCase("7%2", 1)]
        public void ShouldHandleIntegers(string input, int expected)
        {
            var expression = new Expression(input);
            var sut = expression.ToLambda<int>();

            Assert.AreEqual(sut(), expected);
        }

        [Test]
        public void ShouldHandleParameters()
        {
            var expression = new Expression("[FieldA] > 5 && [FieldB] = 'test'");
            var sut = expression.ToLambda<Context, bool>();
            var context = new Context {FieldA = 7, FieldB = "test"};

            Assert.True(sut(context));
        }

        [Test]
        public void ShouldHandleOverloadingSameParamCount() 
        {
            var expression = new Expression("Test('Hello', ' world!')");
            var sut = expression.ToLambda<Context, string>();
            var context = new Context();

            Assert.AreEqual("Hello world!", sut(context));
        }

        [Test]
        public void ShouldHandleOverloadingDifferentParamCount() 
        {
            var expression = new Expression("Test(Test(1, 2), 3, 4)");
            var sut = expression.ToLambda<Context, int>();
            var context = new Context();

            Assert.AreEqual(10, sut(context));
        }

        [Test]
        public void ShouldHandleParamsKeyword() 
        {
            var expression = new Expression("Sum(Test(1,1),2)");
            var sut = expression.ToLambda<Context, int>();
            var context = new Context();

            Assert.AreEqual(4, sut(context));
        }

        [Test]
        public void ShouldHandleMixedParamsKeyword() {
            var expression = new Expression("Sum('Your total is: ', Test(1,1), 2, 3)");
            var sut = expression.ToLambda<Context, string>();
            var context = new Context();

            Assert.AreEqual("Your total is: 7", sut(context));
        }

        [Test]
        public void ShouldHandleCustomFunctions()
        {
            var expression = new Expression("Test(Test(1, 2), 3)");
            var sut = expression.ToLambda<Context, int>();
            var context = new Context();

            Assert.AreEqual(sut(context), 6);
        }

        [Test]
        public void MissingMethod()
        {
            var expression = new Expression("MissingMethod(1)");
            try
            {
                var sut = expression.ToLambda<Context, int>();
            }
            catch(System.MissingMethodException ex)
            {

                System.Diagnostics.Debug.Write(ex);
                Assert.True(true);
                return;
            }
            Assert.True(false);

        }

        [Test]
        public void ShouldHandleTernaryOperator()
        {
            var expression = new Expression("Test(1, 2) = 3 ? 1 : 2");
            var sut = expression.ToLambda<Context, int>();
            var context = new Context();

            Assert.AreEqual(sut(context), 1);
        }

        [Test]
        public void Issue1()
        {
            var expr = new Expression("2 + 2 - a - b - x");

            decimal x = 5m;
            decimal a = 6m;
            decimal b = 7m;

            expr.Parameters["x"] = x;
            expr.Parameters["a"] = a;
            expr.Parameters["b"] = b;

            var f = expr.ToLambda<float>(); // Here it throws System.ArgumentNullException. Parameter name: expression
            Assert.AreEqual(f(), -14);
        }

        [TestCase("if(true, true, false)")]
        [TestCase("in(3, 1, 2, 3, 4)")]
        public void ShouldHandleBuiltInFunctions(string input)
        {
            var expression = new Expression(input);
            var sut = expression.ToLambda<bool>();
            Assert.True(sut());
        }

        [TestCase("[FieldA] > [FieldC]", true)]
        [TestCase("[FieldC] > 1.34", true)]
        [TestCase("[FieldC] > (1.34 * 2) % 3", false)]
        [TestCase("[FieldE] = 2", true)]
        [TestCase("[FieldD] > 0", false)]
        public void ShouldHandleDataConversions(string input, bool expected)
        {
            var expression = new Expression(input);
            var sut = expression.ToLambda<Context, bool>();
            var context = new Context { FieldA = 7, FieldB = "test", FieldC = 2.4m, FieldE = 2 };

            Assert.AreEqual(expected, sut(context));
        }
    }
}
