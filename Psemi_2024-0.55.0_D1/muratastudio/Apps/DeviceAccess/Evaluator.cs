using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Reflection;

namespace DeviceAccess
{
	public class Evaluator
	{
		#region Private Members

		private static object _evaluator = null;
		private static Type _evaluatorType = null;
		private static readonly string _jscriptSource =

			@"package Evaluator
            {
               class Evaluator
               {
                  public function Eval(expr : String) : String 
                  { 
                     return eval(expr); 
                  }
               }
            }";

		#endregion

		#region Constructor

		static Evaluator()
		{
			CodeDomProvider compiler = CodeDomProvider.CreateProvider("JScript");

			CompilerParameters parameters;
			parameters = new CompilerParameters();
			parameters.GenerateInMemory = true;

			CompilerResults results;
			results = compiler.CompileAssemblyFromSource(parameters, _jscriptSource);

			Assembly assembly = results.CompiledAssembly;
			_evaluatorType = assembly.GetType("Evaluator.Evaluator");

			_evaluator = Activator.CreateInstance(_evaluatorType);
		} 

		#endregion

		#region Public Methods

		public static int EvalToInteger(string statement)
		{
			string s = EvalToString(statement);
			return int.Parse(s.ToString(), CultureInfo.InvariantCulture);
		}

		public static double EvalToDouble(string statement)
		{
			string s = EvalToString(statement);
			return double.Parse(s, CultureInfo.InvariantCulture);
		}

		public static decimal EvalToDecimal(string statement)
		{
			string s = EvalToString(statement);
			return decimal.Parse(s, CultureInfo.InvariantCulture);
		}

		public static string EvalToString(string statement)
		{
			object o = EvalToObject(statement);
			return o.ToString();
		}

		public static object EvalToObject(string statement)
		{
			lock (_evaluatorType)
			{
				return _evaluatorType.InvokeMember("Eval", BindingFlags.InvokeMethod, null, _evaluator, new object[] { statement });
			}
		}

		#endregion
	}
}
