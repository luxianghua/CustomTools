using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Test
{
    enum NumberType
    {
        Int32,
        Int64,
        Float,
        Double,
    }

    enum NumberSystem
    {
        Bianry = 2,
        Decimal = 10,
        Hexadecimal = 16
    }

    public class ExecuteStringCode
    {
        Dictionary<string, object> localVariableDics = new Dictionary<string, object>();
        BindingFlags flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

        private static ExecuteStringCode instance;
        public static ExecuteStringCode Instance
        {
            get
            {
                if (instance == null)
                    instance = new ExecuteStringCode();
                return instance;
            }
        }

        public ExecuteStringCode()
        {

        }

        public void Execute(string code)
        {
            localVariableDics.Clear();

            int startIndex = 0;
            ExecuteCode(code, ref startIndex);
        }

        private void ExecuteCode(string code, ref int startIndex)
        {
            do
            {
                ExecuteExpression(code, ref startIndex);
            }
            while (startIndex < code.Length);
        }

        private object ExecuteExpression(string code, ref int startIndex, bool first = true)
        {
            char c = GetNextNonEmptyChar(code, ref startIndex);
            object obj = null;
            if (IsInitialChar(c))
            {
                startIndex--;
                int tempIndex = startIndex;
                var token = GetToken(code, ref startIndex);
                bool isStatic = false;
                if (token == "new")
                {
                    obj = CreateObject(code, ref startIndex);
                    c = GetNextNonEmptyChar(code, ref startIndex);
                }
                else if (token == "true")
                {
                    obj = true;
                    c = GetNextNonEmptyChar(code, ref startIndex);
                }
                else if (token == "false")
                {
                    obj = false;
                    c = GetNextNonEmptyChar(code, ref startIndex);
                }
                else if (token == "null")
                {
                    obj = null;
                    c = GetNextNonEmptyChar(code, ref startIndex);
                }
                else if (token == "if")
                {
                    ExecuteSelection(code, ref startIndex);
                    return null;
                }
                else if (token == "while")
                {
                    ExecuteWhile(code, ref startIndex);
                    return null;
                }
                else if (token == "for")
                {
                    ExecuteFor(code, ref startIndex);
                    return null;
                }
                else if (token == "foreach")
                {
                    ExecuteForeach(code, ref startIndex);
                    return null;
                }
                else if (token == "break")
                {
                    isBreakLoop = true;
                    c = GetNextNonEmptyChar(code, ref startIndex);
                    return null;
                }
                else if (token == "continue")
                {
                    isContuneLoop = true;
                    c = GetNextNonEmptyChar(code, ref startIndex);
                    return null;
                }
                else
                {
                    c = GetNextNonEmptyChar(code, ref startIndex);

                    if (c == '=' && code[startIndex] != '=')
                    {
                        obj = ExecuteExpression(code, ref startIndex);
                        localVariableDics[token] = obj;
                        startIndex--;
                        c = GetNextNonEmptyChar(code, ref startIndex);
                    }
                    else if (!localVariableDics.TryGetValue(token, out obj))
                    {
                        startIndex = tempIndex;
                        obj = GetObjectType(code, ref startIndex);
                        isStatic = true;
                        c = GetNextNonEmptyChar(code, ref startIndex);
                    }
                }

                obj = ExecuteObject(code, ref startIndex, first, obj, c, isStatic);
            }
            else if (c >= '0' && c <= '9')
            {
                startIndex--;
                obj = GetNumber(code, ref startIndex);
                c = GetNextNonEmptyChar(code, ref startIndex);
                obj = ExecuteObject(code, ref startIndex, first, obj, c);
            }
            else if (c == '"')
            {
                startIndex--;
                obj = GetString(code, ref startIndex);
                c = GetNextNonEmptyChar(code, ref startIndex);
                obj = ExecuteObject(code, ref startIndex, first, obj, c);
            }
            else if (c == '\'')
            {
                startIndex--;
                obj = GetChar(code, ref startIndex);
                c = GetNextNonEmptyChar(code, ref startIndex);
                obj = ExecuteObject(code, ref startIndex, first, obj, c);
            }
            else if (c == '(')
            {
                obj = ExecuteExpression(code, ref startIndex);
                c = GetNextNonEmptyChar(code, ref startIndex);
                obj = ExecuteObject(code, ref startIndex, first, obj, c);
            }

            return obj;
        }

        private object CreateObject(string code, ref int startIndex)
        {
            Type type = GetObjectType(code, ref startIndex);
            char c = GetNextNonEmptyChar(code, ref startIndex);
            object obj = null;

            if (c == '(')
            {
                if (type.IsSubclassOf(typeof(Delegate)))
                {
                    var method = GetDelegateMethod(code, ref startIndex);
                    obj = method.CreateDelegate(type, obj);
                }
                else
                {
                    var parameters = GetMethodParameters(code, ref startIndex);
                    obj = Activator.CreateInstance(type, parameters);
                }
            }
            else if (c == '[')
            {
                var count = GetNumber(code, ref startIndex);
                c = GetNextNonEmptyChar(code, ref startIndex);
                obj = Array.CreateInstance(type, (int)count);
            }

            return obj;
        }

        private object ExecuteObject(string code, ref int startIndex, bool first, object obj, char c, bool isStatic = false)
        {
            if (c == '.')
            {
                obj = InvokeMember(obj, code, ref startIndex, isStatic);
            }
            else if (c == '[')
            {
                obj = ExecuteIList(obj, code, ref startIndex);
            }

            startIndex--;
            c = GetNextNonEmptyChar(code, ref startIndex);

            if (c == '=' && code[startIndex] == '=')
            {
                startIndex++;
                var v = ExecuteExpression(code, ref startIndex);
                obj = Equals(obj, v);
                startIndex--;
                c = GetNextNonEmptyChar(code, ref startIndex);
            }
            else if (c == '!' && code[startIndex] == '=')
            {
                startIndex++;
                var v = ExecuteExpression(code, ref startIndex);
                obj = !Equals(obj, v);
                startIndex--;
                c = GetNextNonEmptyChar(code, ref startIndex);
            }
            else if (c == '>')
            {
                if (code[startIndex] == '=')
                {
                    startIndex++;
                    var v = ExecuteExpression(code, ref startIndex);
                    obj = GreaterOrEqual(obj, v);
                }
                else
                {
                    var v = ExecuteExpression(code, ref startIndex);
                    obj = Greater(obj, v);
                }
                startIndex--;
                c = GetNextNonEmptyChar(code, ref startIndex);
            }
            else if (c == '<')
            {
                if (code[startIndex] == '=')
                {
                    startIndex++;
                    var v = ExecuteExpression(code, ref startIndex);
                    obj = LessOrEqual(obj, v);
                }
                else
                {
                    var v = ExecuteExpression(code, ref startIndex);
                    obj = Less(obj, v);
                }
                startIndex--;
                c = GetNextNonEmptyChar(code, ref startIndex);
            }
            else if (c == '+' || c == '-' || c == '*' || c == '/' || c == '%')
            {
                if (first)
                {
                    obj = Operate(obj, c, code, ref startIndex);
                    startIndex--;
                    c = GetNextNonEmptyChar(code, ref startIndex);
                }
            }
            else if (c == '|' || c == '&')
            {
                if (first)
                {
                    obj = Operate(obj, c, code, ref startIndex);
                    startIndex--;
                    c = GetNextNonEmptyChar(code, ref startIndex);
                }
            }

            if (c == '+' || c == '-' || c == '*' || c == '/' || c == '%')
            {
            }
            else if (c == '|' || c == '&')
            {

            }
            else if (c == ';')
            {
            }
            else if (c == ',')
            {
            }
            else if (c == ')')
            {
            }
            else if (c == ']')
            {
            }
            else
                throw new Exception($"The operator is incorrect. ({c})");

            return obj;
        }

        private object ExecuteIList(object obj, string code, ref int startIndex)
        {
            var key = ExecuteExpression(code, ref startIndex);
            char c = GetNextNonEmptyChar(code, ref startIndex);
            
            if (c == '=' && code[startIndex] != '=')
            {
                if (obj is IList list)
                {
                    obj = ExecuteExpression(code, ref startIndex);
                    list[(int)key] = obj;
                }
                else if (obj is IDictionary dict)
                {
                    obj = ExecuteExpression(code, ref startIndex);
                    dict[key] = obj;
                }
            }
            else
            {
                if (obj is IList list)
                    obj = list[(int)key];
                else if (obj is IDictionary dict)
                    obj = dict[key];

                if (c == '.')
                {
                    obj = InvokeMember(obj, code, ref startIndex);
                }
                else if (c == '[')
                {
                    obj = ExecuteIList(obj, code, ref startIndex);
                }
            }

            return obj;
        }

        private void ExecuteSelection(string code, ref int startIndex)
        {
            char c = GetNextNonEmptyChar(code, ref startIndex);
            if (c == '(')
            {
                var obj = ExecuteExpression(code, ref startIndex);
                c = GetNextNonEmptyChar(code, ref startIndex);
                if (c == '{')
                {
                    if ((bool)obj)
                    {
                        c = GetNextNonEmptyChar(code, ref startIndex);
                        while (c != '}')
                        {
                            startIndex--;
                            ExecuteExpression(code, ref startIndex);
                            c = GetNextNonEmptyChar(code, ref startIndex);
                        }
                    }
                    else
                    {
                        SkipClosure(code, ref startIndex);
                    }
                }
                else
                {
                    startIndex--;
                    if ((bool)obj)
                        ExecuteExpression(code, ref startIndex);
                    else
                        SkipSingleExpression(code, ref startIndex);
                }

                int index = startIndex;
                string token = GetToken(code, ref startIndex);
                if (token == "else")
                {
                    c = GetNextNonEmptyChar(code, ref startIndex);
                    if (c == '{')
                    {
                        if ((bool)obj == false)
                        {
                            c = GetNextNonEmptyChar(code, ref startIndex);
                            while (c != '}')
                            {
                                startIndex--;
                                ExecuteExpression(code, ref startIndex);
                                c = GetNextNonEmptyChar(code, ref startIndex);
                            }
                        }
                        else
                        {
                            SkipClosure(code, ref startIndex);
                        }
                    }
                    else
                    {
                        startIndex--;
                        if ((bool)obj == false)
                            ExecuteExpression(code, ref startIndex);
                        else
                            SkipSingleExpression(code, ref startIndex);
                    }
                }
                else
                    startIndex = index;
            }
            else
                throw new Exception("Selection statement format is incorrect!");
        }

        bool isBreakLoop = false;
        bool isContuneLoop = false;

        private void ExecuteWhile(string code, ref int startIndex)
        {
            char c = GetNextNonEmptyChar(code, ref startIndex);
            if (c == '(')
            {
                int conditionIndex = startIndex;
                object obj = ExecuteExpression(code, ref startIndex);
                int breakIndex = startIndex;
                while ((bool)obj)
                {
                    c = GetNextNonEmptyChar(code, ref startIndex);
                    if (c == '{')
                    {
                        c = GetNextNonEmptyChar(code, ref startIndex);
                        while (c != '}')
                        {
                            startIndex--;
                            ExecuteExpression(code, ref startIndex);
                            if (isBreakLoop)
                                break;
                            if (isContuneLoop)
                            {
                                isContuneLoop = false;
                                break;
                            }
                            c = GetNextNonEmptyChar(code, ref startIndex);
                        }
                        if (isBreakLoop)
                        {
                            startIndex = breakIndex;
                            isBreakLoop = false;
                            break;
                        }
                        else
                        {
                            startIndex = conditionIndex;
                            obj = ExecuteExpression(code, ref startIndex);
                        }
                    }
                    else
                        throw new Exception("While statement format is incorrect!");
                }
                c = GetNextNonEmptyChar(code, ref startIndex);
                SkipClosure(code, ref startIndex);
            }
            else
                throw new Exception("While statement format is incorrect!");
        }

        private void ExecuteFor(string code, ref int startIndex)
        {
            char c = GetNextNonEmptyChar(code, ref startIndex);
            if (c == '(')
            {
                ExecuteExpression(code, ref startIndex);
                int secondExpressionIndex = startIndex;
                object obj = ExecuteExpression(code, ref startIndex);
                int thirdExpressionIndex = startIndex;
                SkipForThirdExpression(code, ref startIndex);
                int breakIndex = startIndex;
                while ((bool)obj)
                {
                    c = GetNextNonEmptyChar(code, ref startIndex);
                    if (c == '{')
                    {
                        c = GetNextNonEmptyChar(code, ref startIndex);
                        while (c != '}')
                        {
                            startIndex--;
                            ExecuteExpression(code, ref startIndex);
                            if (isBreakLoop)
                                break;
                            if (isContuneLoop)
                            {
                                isContuneLoop = false;
                                break;
                            }
                            c = GetNextNonEmptyChar(code, ref startIndex);
                        }

                        if (isBreakLoop)
                        {
                            startIndex = breakIndex;
                            isBreakLoop = false;
                            break;
                        }
                        else
                        {
                            startIndex = thirdExpressionIndex;
                            ExecuteExpression(code, ref startIndex);
                            startIndex = secondExpressionIndex;
                            obj = ExecuteExpression(code, ref startIndex);
                            SkipForThirdExpression(code, ref startIndex);
                        }
                    }
                    else
                        throw new Exception("For statement format is incorrect!");
                }
                c = GetNextNonEmptyChar(code, ref startIndex);
                SkipClosure(code, ref startIndex);
            }
            else
                throw new Exception("For statement format is incorrect!");
        }

        private void ExecuteForeach(string code, ref int startIndex)
        {
            char c = GetNextNonEmptyChar(code, ref startIndex);
            if (c == '(')
            {
                string token = GetToken(code, ref startIndex);
                string token1 = GetToken(code, ref startIndex);
                if (token1 == "in")
                {
                    object obj = ExecuteExpression(code, ref startIndex);
                    IEnumerable enumerable = obj as IEnumerable;
                    if (obj != null)
                    {
                        var enumerator = enumerable.GetEnumerator();
                        if (enumerator != null)
                        {
                            int breakIndex = startIndex;
                            while (enumerator.MoveNext())
                            {
                                localVariableDics[token] = enumerator.Current;
                                c = GetNextNonEmptyChar(code, ref startIndex);
                                if (c == '{')
                                {
                                    c = GetNextNonEmptyChar(code, ref startIndex);
                                    while (c != '}')
                                    {
                                        startIndex--;
                                        ExecuteExpression(code, ref startIndex);
                                        if (isBreakLoop)
                                            break;
                                        if (isContuneLoop)
                                        {
                                            isContuneLoop = false;
                                            break;
                                        }
                                        c = GetNextNonEmptyChar(code, ref startIndex);
                                    }

                                    startIndex = breakIndex;
                                    if (isBreakLoop)
                                    {
                                        isBreakLoop = false;
                                        break;
                                    }
                                }
                                else
                                    throw new Exception("Foreach statement format is incorrect!");
                            }

                            c = GetNextNonEmptyChar(code, ref startIndex);
                            SkipClosure(code, ref startIndex);
                        }
                        else
                            throw new Exception("Enumerator is null!");
                    }
                    else
                        throw new Exception("obj is not IEnumerable!");
                }
                else
                    throw new Exception("Foreach statement format is incorrect!");
            }
            else
                throw new Exception("Foreach statement format is incorrect!");
        }

        private object Operate(object obj, char c, string code, ref int startIndex)
        {
            if (c == '+')
                obj = Plus(obj, code, ref startIndex);
            else if (c == '-')
                obj = Minus(obj, code, ref startIndex);
            else if (c == '*')
            {
                obj = Multiply(obj, code, ref startIndex);
                startIndex--;
                c = GetNextNonEmptyChar(code, ref startIndex);
                obj = Operate(obj, c, code, ref startIndex);
            }
            else if (c == '/')
            {
                obj = Divide(obj, code, ref startIndex);
                startIndex--;
                c = GetNextNonEmptyChar(code, ref startIndex);
                obj = Operate(obj, c, code, ref startIndex);
            }
            else if (c == '%')
            {
                obj = Remainder(obj, code, ref startIndex);
                startIndex--;
                c = GetNextNonEmptyChar(code, ref startIndex);
                obj = Operate(obj, c, code, ref startIndex);
            }
            else if (c == '|')
            {
                if (code[startIndex] == '|')
                {
                    startIndex++;
                    obj = OROp(obj, code, ref startIndex);
                }
                else
                {
                    obj = BitWiseOR(obj, code, ref startIndex);
                }
            }
            else if (c == '&')
            {
                if (code[startIndex] == '&')
                {
                    startIndex++;
                    obj = ANDOp(obj, code, ref startIndex);
                    startIndex--;
                    c = GetNextNonEmptyChar(code, ref startIndex);
                    obj = Operate(obj, c, code, ref startIndex);
                }
                else
                {
                    obj = BitWiseAND(obj, code, ref startIndex);
                    startIndex--;
                    c = GetNextNonEmptyChar(code, ref startIndex);
                    obj = Operate(obj, c, code, ref startIndex);
                }
            }

            return obj;
        }

        private object Plus(object obj, string code, ref int startIndex)
        {
            char c = GetNextNonEmptyChar(code, ref startIndex);
            object v;
            if (c == '(')
            {
                v = ExecuteExpression(code, ref startIndex);
                startIndex++;
            }
            else
            {
                startIndex--;
                v = ExecuteExpression(code, ref startIndex, false);
            }
            
            startIndex--;
            c = GetNextNonEmptyChar(code, ref startIndex);

            if (c == '*')
            {
                v = Multiply(v, code, ref startIndex);
                startIndex--;
                c = GetNextNonEmptyChar(code, ref startIndex);
            }
            else if (c == '/')
            {
                v = Divide(v, code, ref startIndex);
                startIndex--;
                c = GetNextNonEmptyChar(code, ref startIndex);
            }
            else if (c == '%')
            {
                v = Remainder(v, code, ref startIndex);
                startIndex--;
                c = GetNextNonEmptyChar(code, ref startIndex);
            }

            obj = PlusTwoObjects(obj, v);

            if (c == '+')
            {
                obj = Plus(obj, code, ref startIndex);
            }
            else if (c == '-')
            {
                obj = Minus(obj, code, ref startIndex);
            }

            return obj;
        }

        private object Minus(object obj, string code, ref int startIndex)
        {
            char c = GetNextNonEmptyChar(code, ref startIndex);
            object v;
            if (c == '(')
            {
                v = ExecuteExpression(code, ref startIndex);
                startIndex++;
            }
            else
            {
                startIndex--;
                v = ExecuteExpression(code, ref startIndex, false);
            }

            startIndex--;
            c = GetNextNonEmptyChar(code, ref startIndex);

            if (c == '*')
            {
                v = Multiply(v, code, ref startIndex);
                startIndex--;
                c = GetNextNonEmptyChar(code, ref startIndex);
            }
            else if (c == '/')
            {
                v = Divide(v, code, ref startIndex);
                startIndex--;
                c = GetNextNonEmptyChar(code, ref startIndex);
            }
            else if (c == '%')
            {
                v = Remainder(v, code, ref startIndex);
                startIndex--;
                c = GetNextNonEmptyChar(code, ref startIndex);
            }

            obj = MinusTwoObjects(obj, v);

            if (c == '+')
            {
                obj = Plus(obj, code, ref startIndex);
            }
            else if (c == '-')
            {
                obj = Minus(obj, code, ref startIndex);
            }

            return obj;
        }

        private object Multiply(object obj, string code, ref int startIndex)
        {
            char c = GetNextNonEmptyChar(code, ref startIndex);
            object v;
            if (c == '(')
            {
                v = ExecuteExpression(code, ref startIndex);
                startIndex++;
            }
            else
            {
                startIndex--;
                v = ExecuteExpression(code, ref startIndex, false);
            }

            startIndex--;
            c = GetNextNonEmptyChar(code, ref startIndex);

            obj = MultiplyTwoObjects(obj, v);

            if (c == '*')
            {
                obj = Multiply(obj, code, ref startIndex);
            }
            else if (c == '/')
            {
                obj = Divide(obj, code, ref startIndex);
            }
            else if (c == '%')
            {
                obj = Remainder(obj, code, ref startIndex);
            }
            return obj;
        }

        private object Divide(object obj, string code, ref int startIndex)
        {
            char c = GetNextNonEmptyChar(code, ref startIndex);
            object v;
            if (c == '(')
            {
                v = ExecuteExpression(code, ref startIndex);
                startIndex++;
            }
            else
            {
                startIndex--;
                v = ExecuteExpression(code, ref startIndex, false);
            }

            startIndex--;
            c = GetNextNonEmptyChar(code, ref startIndex);

            obj = DivideTwoObjects(obj, v);

            if (c == '*')
            {
                obj = Multiply(obj, code, ref startIndex);
            }
            else if (c == '/')
            {
                obj = Divide(obj, code, ref startIndex);
            }
            else if (c == '%')
            {
                obj = Remainder(obj, code, ref startIndex);
            }
            return obj;
        }

        private object Remainder(object obj, string code, ref int startIndex)
        {
            char c = GetNextNonEmptyChar(code, ref startIndex);
            object v;
            if (c == '(')
            {
                v = ExecuteExpression(code, ref startIndex);
                startIndex++;
            }
            else
            {
                startIndex--;
                v = ExecuteExpression(code, ref startIndex, false);
            }

            startIndex--;
            c = GetNextNonEmptyChar(code, ref startIndex);

            obj = RemainderTwoObjects(obj, v);

            if (c == '*')
            {
                obj = Multiply(obj, code, ref startIndex);
            }
            else if (c == '/')
            {
                obj = Divide(obj, code, ref startIndex);
            }
            else if (c == '%')
            {
                obj = Remainder(obj, code, ref startIndex);
            }
            return obj;
        }

        private object PlusTwoObjects(object a, object b)
        {
            if (a is string || b is string)
            {
                return a?.ToString() + b?.ToString();
            }
            else if (a is int)
            {
                if (b is int)
                    return (int)a + (int)b;
                else if (b is long)
                    return (int)a + (long)b;
                else if (b is float)
                    return (int)a + (float)b;
                else if (b is double)
                    return (int)a + (double)b;
                else if (b is byte)
                    return (int)a + (byte)b;
            }
            else if (a is long)
            {
                if (b is int)
                    return (long)a + (int)b;
                else if (b is long)
                    return (long)a + (long)b;
                else if (b is float)
                    return (long)a + (float)b;
                else if (b is double)
                    return (long)a + (double)b;
                else if (b is byte)
                    return (long)a + (byte)b;
            }
            else if (a is float)
            {
                if (b is int)
                    return (float)a + (int)b;
                else if (b is long)
                    return (float)a + (long)b;
                else if (b is float)
                    return (float)a + (float)b;
                else if (b is double)
                    return (float)a + (double)b;
                else if (b is byte)
                    return (float)a + (byte)b;
            }
            else if (a is double)
            {
                if (b is int)
                    return (double)a + (int)b;
                else if (b is long)
                    return (double)a + (long)b;
                else if (b is float)
                    return (double)a + (float)b;
                else if (b is double)
                    return (double)a + (double)b;
                else if (b is byte)
                    return (double)a + (byte)b;
            }
            else if (a is byte)
            {
                if (b is int)
                    return (byte)a + (int)b;
                else if (b is long)
                    return (byte)a + (long)b;
                else if (b is float)
                    return (byte)a + (float)b;
                else if (b is double)
                    return (byte)a + (double)b;
                else if (b is byte)
                    return (byte)a + (byte)b;
            }

            throw new Exception($"a ({a}) don't PlusTwoObject b ({b})");
        }

        private object MinusTwoObjects(object a, object b)
        {
            if (a is int)
            {
                if (b is int)
                    return (int)a - (int)b;
                else if (b is long)
                    return (int)a - (long)b;
                else if (b is float)
                    return (int)a - (float)b;
                else if (b is double)
                    return (int)a - (double)b;
                else if (b is byte)
                    return (int)a - (byte)b;
            }
            else if (a is long)
            {
                if (b is int)
                    return (long)a - (int)b;
                else if (b is long)
                    return (long)a - (long)b;
                else if (b is float)
                    return (long)a - (float)b;
                else if (b is double)
                    return (long)a - (double)b;
                else if (b is byte)
                    return (long)a - (byte)b;
            }
            else if (a is float)
            {
                if (b is int)
                    return (float)a - (int)b;
                else if (b is long)
                    return (float)a - (long)b;
                else if (b is float)
                    return (float)a - (float)b;
                else if (b is double)
                    return (float)a - (double)b;
                else if (b is byte)
                    return (float)a - (byte)b;
            }
            else if (a is double)
            {
                if (b is int)
                    return (double)a - (int)b;
                else if (b is long)
                    return (double)a - (long)b;
                else if (b is float)
                    return (double)a - (float)b;
                else if (b is double)
                    return (double)a - (double)b;
                else if (b is byte)
                    return (double)a - (byte)b;
            }
            else if (a is byte)
            {
                if (b is int)
                    return (byte)a - (int)b;
                else if (b is long)
                    return (byte)a - (long)b;
                else if (b is float)
                    return (byte)a - (float)b;
                else if (b is double)
                    return (byte)a - (double)b;
                else if (b is byte)
                    return (byte)a - (byte)b;
            }

            throw new Exception($"a ({a}) don't MinusTwoObject b ({b})");
        }

        private object MultiplyTwoObjects(object a, object b)
        {
            if (a is int)
            {
                if (b is int)
                    return (int)a * (int)b;
                else if (b is long)
                    return (int)a * (long)b;
                else if (b is float)
                    return (int)a * (float)b;
                else if (b is double)
                    return (int)a * (double)b;
                else if (b is byte)
                    return (int)a * (byte)b;
            }
            else if (a is long)
            {
                if (b is int)
                    return (long)a * (int)b;
                else if (b is long)
                    return (long)a * (long)b;
                else if (b is float)
                    return (long)a * (float)b;
                else if (b is double)
                    return (long)a * (double)b;
                else if (b is byte)
                    return (long)a * (byte)b;
            }
            else if (a is float)
            {
                if (b is int)
                    return (float)a * (int)b;
                else if (b is long)
                    return (float)a * (long)b;
                else if (b is float)
                    return (float)a * (float)b;
                else if (b is double)
                    return (float)a * (double)b;
                else if (b is byte)
                    return (float)a * (byte)b;
            }
            else if (a is double)
            {
                if (b is int)
                    return (double)a * (int)b;
                else if (b is long)
                    return (double)a * (long)b;
                else if (b is float)
                    return (double)a * (float)b;
                else if (b is double)
                    return (double)a * (double)b;
                else if (b is byte)
                    return (double)a * (byte)b;
            }
            else if (a is byte)
            {
                if (b is int)
                    return (byte)a * (int)b;
                else if (b is long)
                    return (byte)a * (long)b;
                else if (b is float)
                    return (byte)a * (float)b;
                else if (b is double)
                    return (byte)a * (double)b;
                else if (b is byte)
                    return (byte)a * (byte)b;
            }

            throw new Exception($"a ({a}) don't MultiplyTwoObject b ({b})");
        }

        private object DivideTwoObjects(object a, object b)
        {
            if (a is int)
            {
                if (b is int)
                    return (int)a / (int)b;
                else if (b is long)
                    return (int)a / (long)b;
                else if (b is float)
                    return (int)a / (float)b;
                else if (b is double)
                    return (int)a / (double)b;
                else if (b is byte)
                    return (int)a / (byte)b;
            }
            else if (a is long)
            {
                if (b is int)
                    return (long)a / (int)b;
                else if (b is long)
                    return (long)a / (long)b;
                else if (b is float)
                    return (long)a / (float)b;
                else if (b is double)
                    return (long)a / (double)b;
                else if (b is byte)
                    return (long)a / (byte)b;
            }
            else if (a is float)
            {
                if (b is int)
                    return (float)a / (int)b;
                else if (b is long)
                    return (float)a / (long)b;
                else if (b is float)
                    return (float)a / (float)b;
                else if (b is double)
                    return (float)a / (double)b;
                else if (b is byte)
                    return (float)a / (byte)b;
            }
            else if (a is double)
            {
                if (b is int)
                    return (double)a / (int)b;
                else if (b is long)
                    return (double)a / (long)b;
                else if (b is float)
                    return (double)a / (float)b;
                else if (b is double)
                    return (double)a / (double)b;
                else if (b is byte)
                    return (double)a / (byte)b;
            }
            else if (a is byte)
            {
                if (b is int)
                    return (byte)a / (int)b;
                else if (b is long)
                    return (byte)a / (long)b;
                else if (b is float)
                    return (byte)a / (float)b;
                else if (b is double)
                    return (byte)a / (double)b;
                else if (b is byte)
                    return (byte)a / (byte)b;
            }

            throw new Exception($"a ({a}) don't DivideTwoObject b ({b})");
        }

        private object RemainderTwoObjects(object a, object b)
        {
            if (a is int)
            {
                if (b is int)
                    return (int)a % (int)b;
                else if (b is long)
                    return (int)a % (long)b;
                else if (b is float)
                    return (int)a % (float)b;
                else if (b is double)
                    return (int)a % (double)b;
                else if (b is byte)
                    return (int)a % (byte)b;
            }
            else if (a is long)
            {
                if (b is int)
                    return (long)a % (int)b;
                else if (b is long)
                    return (long)a % (long)b;
                else if (b is float)
                    return (long)a % (float)b;
                else if (b is double)
                    return (long)a % (double)b;
                else if (b is byte)
                    return (long)a % (byte)b;
            }
            else if (a is float)
            {
                if (b is int)
                    return (float)a % (int)b;
                else if (b is long)
                    return (float)a % (long)b;
                else if (b is float)
                    return (float)a % (float)b;
                else if (b is double)
                    return (float)a % (double)b;
                else if (b is byte)
                    return (float)a % (byte)b;
            }
            else if (a is double)
            {
                if (b is int)
                    return (double)a % (int)b;
                else if (b is long)
                    return (double)a % (long)b;
                else if (b is float)
                    return (double)a % (float)b;
                else if (b is double)
                    return (double)a % (double)b;
                else if (b is byte)
                    return (double)a % (byte)b;
            }
            else if (a is byte)
            {
                if (b is int)
                    return (byte)a % (int)b;
                else if (b is long)
                    return (byte)a % (long)b;
                else if (b is float)
                    return (byte)a % (float)b;
                else if (b is double)
                    return (byte)a % (double)b;
                else if (b is byte)
                    return (byte)a % (byte)b;
            }

            throw new Exception($"a ({a}) don't DivideTwoObject b ({b})");
        }

        private object Greater(object a, object b)
        {
            if (a is int)
            {
                if (b is int)
                    return (int)a > (int)b;
                else if (b is long)
                    return (int)a > (long)b;
                else if (b is float)
                    return (int)a > (float)b;
                else if (b is double)
                    return (int)a > (double)b;
                else if (b is byte)
                    return (int)a > (byte)b;
                else if (b is char)
                    return (int)a > (char)b;
            }
            else if (a is long)
            {
                if (b is int)
                    return (long)a > (int)b;
                else if (b is long)
                    return (long)a > (long)b;
                else if (b is float)
                    return (long)a > (float)b;
                else if (b is double)
                    return (long)a > (double)b;
                else if (b is byte)
                    return (long)a > (byte)b;
                else if (b is char)
                    return (long)a > (char)b;
            }
            else if (a is float)
            {
                if (b is int)
                    return (float)a > (int)b;
                else if (b is long)
                    return (float)a > (long)b;
                else if (b is float)
                    return (float)a > (float)b;
                else if (b is double)
                    return (float)a > (double)b;
                else if (b is byte)
                    return (float)a > (byte)b;
                else if (b is char)
                    return (float)a > (char)b;
            }
            else if (a is double)
            {
                if (b is int)
                    return (double)a > (int)b;
                else if (b is long)
                    return (double)a > (long)b;
                else if (b is float)
                    return (double)a > (float)b;
                else if (b is double)
                    return (double)a > (double)b;
                else if (b is byte)
                    return (double)a > (byte)b;
                else if (b is char)
                    return (double)a > (char)b;
            }
            else if (a is byte)
            {
                if (b is int)
                    return (byte)a > (int)b;
                else if (b is long)
                    return (byte)a > (long)b;
                else if (b is float)
                    return (byte)a > (float)b;
                else if (b is double)
                    return (byte)a > (double)b;
                else if (b is byte)
                    return (byte)a > (byte)b;
                else if (b is char)
                    return (byte)a > (char)b;
            }
            else if (a is char)
            {
                if (b is int)
                    return (char)a > (int)b;
                else if (b is long)
                    return (char)a > (long)b;
                else if (b is float)
                    return (char)a > (float)b;
                else if (b is double)
                    return (char)a > (double)b;
                else if (b is byte)
                    return (char)a > (byte)b;
                else if (b is char)
                    return (char)a > (char)b;
            }

            throw new Exception($"a ({a}) don't Greater b ({b})");
        }

        private object GreaterOrEqual(object a, object b)
        {
            if (a is int)
            {
                if (b is int)
                    return (int)a >= (int)b;
                else if (b is long)
                    return (int)a >= (long)b;
                else if (b is float)
                    return (int)a >= (float)b;
                else if (b is double)
                    return (int)a >= (double)b;
                else if (b is byte)
                    return (int)a >= (byte)b;
                else if (b is char)
                    return (int)a >= (char)b;
            }
            else if (a is long)
            {
                if (b is int)
                    return (long)a >= (int)b;
                else if (b is long)
                    return (long)a >= (long)b;
                else if (b is float)
                    return (long)a >= (float)b;
                else if (b is double)
                    return (long)a >= (double)b;
                else if (b is byte)
                    return (long)a >= (byte)b;
                else if (b is char)
                    return (long)a >= (char)b;
            }
            else if (a is float)
            {
                if (b is int)
                    return (float)a >= (int)b;
                else if (b is long)
                    return (float)a >= (long)b;
                else if (b is float)
                    return (float)a >= (float)b;
                else if (b is double)
                    return (float)a >= (double)b;
                else if (b is byte)
                    return (float)a >= (byte)b;
                else if (b is char)
                    return (float)a >= (char)b;
            }
            else if (a is double)
            {
                if (b is int)
                    return (double)a >= (int)b;
                else if (b is long)
                    return (double)a >= (long)b;
                else if (b is float)
                    return (double)a >= (float)b;
                else if (b is double)
                    return (double)a >= (double)b;
                else if (b is byte)
                    return (double)a >= (byte)b;
                else if (b is char)
                    return (double)a >= (char)b;
            }
            else if (a is byte)
            {
                if (b is int)
                    return (byte)a >= (int)b;
                else if (b is long)
                    return (byte)a >= (long)b;
                else if (b is float)
                    return (byte)a >= (float)b;
                else if (b is double)
                    return (byte)a >= (double)b;
                else if (b is byte)
                    return (byte)a >= (byte)b;
                else if (b is char)
                    return (byte)a >= (char)b;
            }
            else if (a is char)
            {
                if (b is int)
                    return (char)a >= (int)b;
                else if (b is long)
                    return (char)a >= (long)b;
                else if (b is float)
                    return (char)a >= (float)b;
                else if (b is double)
                    return (char)a >= (double)b;
                else if (b is byte)
                    return (char)a >= (byte)b;
                else if (b is char)
                    return (char)a >= (char)b;
            }

            throw new Exception($"a ({a}) don't GreaterOrEqual b ({b})");
        }

        private object Less(object a, object b)
        {
            if (a is int)
            {
                if (b is int)
                    return (int)a < (int)b;
                else if (b is long)
                    return (int)a < (long)b;
                else if (b is float)
                    return (int)a < (float)b;
                else if (b is double)
                    return (int)a < (double)b;
                else if (b is byte)
                    return (int)a < (byte)b;
                else if (b is char)
                    return (int)a < (char)b;
            }
            else if (a is long)
            {
                if (b is int)
                    return (long)a < (int)b;
                else if (b is long)
                    return (long)a < (long)b;
                else if (b is float)
                    return (long)a < (float)b;
                else if (b is double)
                    return (long)a < (double)b;
                else if (b is byte)
                    return (long)a < (byte)b;
                else if (b is char)
                    return (long)a < (char)b;
            }
            else if (a is float)
            {
                if (b is int)
                    return (float)a < (int)b;
                else if (b is long)
                    return (float)a < (long)b;
                else if (b is float)
                    return (float)a < (float)b;
                else if (b is double)
                    return (float)a < (double)b;
                else if (b is byte)
                    return (float)a < (byte)b;
                else if (b is char)
                    return (float)a < (char)b;
            }
            else if (a is double)
            {
                if (b is int)
                    return (double)a < (int)b;
                else if (b is long)
                    return (double)a < (long)b;
                else if (b is float)
                    return (double)a < (float)b;
                else if (b is double)
                    return (double)a < (double)b;
                else if (b is byte)
                    return (double)a < (byte)b;
                else if (b is char)
                    return (double)a < (char)b;
            }
            else if (a is byte)
            {
                if (b is int)
                    return (byte)a < (int)b;
                else if (b is long)
                    return (byte)a < (long)b;
                else if (b is float)
                    return (byte)a < (float)b;
                else if (b is double)
                    return (byte)a < (double)b;
                else if (b is byte)
                    return (byte)a < (byte)b;
                else if (b is char)
                    return (byte)a < (char)b;
            }
            else if (a is char)
            {
                if (b is int)
                    return (char)a < (int)b;
                else if (b is long)
                    return (char)a < (long)b;
                else if (b is float)
                    return (char)a < (float)b;
                else if (b is double)
                    return (char)a < (double)b;
                else if (b is byte)
                    return (char)a < (byte)b;
                else if (b is char)
                    return (char)a < (char)b;
            }

            throw new Exception($"a ({a}) don't Less b ({b})");
        }

        private object LessOrEqual(object a, object b)
        {
            if (a is int)
            {
                if (b is int)
                    return (int)a <= (int)b;
                else if (b is long)
                    return (int)a <= (long)b;
                else if (b is float)
                    return (int)a <= (float)b;
                else if (b is double)
                    return (int)a <= (double)b;
                else if (b is byte)
                    return (int)a <= (byte)b;
                else if (b is char)
                    return (int)a <= (char)b;
            }
            else if (a is long)
            {
                if (b is int)
                    return (long)a <= (int)b;
                else if (b is long)
                    return (long)a <= (long)b;
                else if (b is float)
                    return (long)a <= (float)b;
                else if (b is double)
                    return (long)a <= (double)b;
                else if (b is byte)
                    return (long)a <= (byte)b;
                else if (b is char)
                    return (long)a <= (char)b;
            }
            else if (a is float)
            {
                if (b is int)
                    return (float)a <= (int)b;
                else if (b is long)
                    return (float)a <= (long)b;
                else if (b is float)
                    return (float)a <= (float)b;
                else if (b is double)
                    return (float)a <= (double)b;
                else if (b is byte)
                    return (float)a <= (byte)b;
                else if (b is char)
                    return (float)a <= (char)b;
            }
            else if (a is double)
            {
                if (b is int)
                    return (double)a <= (int)b;
                else if (b is long)
                    return (double)a <= (long)b;
                else if (b is float)
                    return (double)a <= (float)b;
                else if (b is double)
                    return (double)a <= (double)b;
                else if (b is byte)
                    return (double)a <= (byte)b;
                else if (b is char)
                    return (double)a <= (char)b;
            }
            else if (a is byte)
            {
                if (b is int)
                    return (byte)a <= (int)b;
                else if (b is long)
                    return (byte)a <= (long)b;
                else if (b is float)
                    return (byte)a <= (float)b;
                else if (b is double)
                    return (byte)a <= (double)b;
                else if (b is byte)
                    return (byte)a <= (byte)b;
                else if (b is char)
                    return (byte)a <= (char)b;
            }
            else if (a is char)
            {
                if (b is int)
                    return (char)a <= (int)b;
                else if (b is long)
                    return (char)a <= (long)b;
                else if (b is float)
                    return (char)a <= (float)b;
                else if (b is double)
                    return (char)a <= (double)b;
                else if (b is byte)
                    return (char)a <= (byte)b;
                else if (b is char)
                    return (char)a <= (char)b;
            }

            throw new Exception($"a ({a}) don't LessOrEqual b ({b})");
        }

        private object ANDOp(object obj, string code, ref int startIndex)
        {
            if ((bool)obj == false)
            {
                SkipANDExpression(code, ref startIndex);
                return obj;
            }

            char c = GetNextNonEmptyChar(code, ref startIndex);
            object v;
            if (c == '(')
            {
                v = ExecuteExpression(code, ref startIndex);
                startIndex++;
            }
            else
            {
                startIndex--;
                v = ExecuteExpression(code, ref startIndex, false);
            }

            startIndex--;
            c = GetNextNonEmptyChar(code, ref startIndex);

            obj = (bool)obj && (bool)v;

            if (c == '&' && code[startIndex] == '&')
            {
                startIndex++;
                obj = ANDOp(obj, code, ref startIndex);
            }

            return obj;
        }

        private object OROp(object obj, string code, ref int startIndex)
        {
            if ((bool)obj)
            {
                SkipORExpression(code, ref startIndex);
                return obj;
            }

            char c = GetNextNonEmptyChar(code, ref startIndex);
            object v;
            if (c == '(')
            {
                v = ExecuteExpression(code, ref startIndex);
                startIndex++;
            }
            else
            {
                startIndex--;
                v = ExecuteExpression(code, ref startIndex, false);
            }

            startIndex--;
            c = GetNextNonEmptyChar(code, ref startIndex);

            if (c == '&' && code[startIndex] == '&')
            {
                v = ANDOp(obj, code, ref startIndex);
                startIndex--;
                c = GetNextNonEmptyChar(code, ref startIndex);
            }

            obj = (bool)obj || (bool)v;

            if (c == '|' && code[startIndex] == '|')
            {
                startIndex++;
                obj = OROp(obj, code, ref startIndex);
            }

            return obj;
        }

        private object BitWiseAND(object obj, string code, ref int startIndex)
        {
            char c = GetNextNonEmptyChar(code, ref startIndex);
            object v;
            if (c == '(')
            {
                v = ExecuteExpression(code, ref startIndex);
                startIndex++;
            }
            else
            {
                startIndex--;
                v = ExecuteExpression(code, ref startIndex, false);
            }

            startIndex--;
            c = GetNextNonEmptyChar(code, ref startIndex);

            obj = ANDOpTwoObjects(obj, v);

            if (c == '&' && code[startIndex] != '&')
            {
                obj = BitWiseAND(obj, code, ref startIndex);
            }

            return obj;
        }

        private object BitWiseOR(object obj, string code, ref int startIndex)
        {
            char c = GetNextNonEmptyChar(code, ref startIndex);
            object v;
            if (c == '(')
            {
                v = ExecuteExpression(code, ref startIndex);
                startIndex++;
            }
            else
            {
                startIndex--;
                v = ExecuteExpression(code, ref startIndex, false);
            }

            startIndex--;
            c = GetNextNonEmptyChar(code, ref startIndex);

            if (c == '&' && code[startIndex] != '&')
            {
                v = BitWiseAND(obj, code, ref startIndex);
                startIndex--;
                c = GetNextNonEmptyChar(code, ref startIndex);
            }

            obj = OROpTwoObjects(obj, v);

            if (c == '|' && code[startIndex] != '|')
            {
                obj = BitWiseOR(obj, code, ref startIndex);
            }

            return obj;
        }

        private object ANDOpTwoObjects(object a, object b)
        {
            if (a is int)
            {
                if (b is int)
                    return (int)a & (int)b;
                else if (b is long)
                    return (int)a & (long)b;
                else if (b is byte)
                    return (int)a & (byte)b;
                else if (b is char)
                    return (int)a & (char)b;
            }
            else if (a is long)
            {
                if (b is int)
                    return (long)a & (int)b;
                else if (b is long)
                    return (long)a & (long)b;
                else if (b is byte)
                    return (long)a & (byte)b;
                else if (b is char)
                    return (long)a & (char)b;
            }
            else if (a is byte)
            {
                if (b is int)
                    return (byte)a & (int)b;
                else if (b is long)
                    return (byte)a & (long)b;
                else if (b is byte)
                    return (byte)a & (byte)b;
                else if (b is char)
                    return (byte)a & (char)b;
            }
            else if (a is char)
            {
                if (b is int)
                    return (char)a & (int)b;
                else if (b is long)
                    return (char)a & (long)b;
                else if (b is byte)
                    return (char)a & (byte)b;
                else if (b is char)
                    return (char)a & (char)b;
            }
            else if (a is bool)
            {
                if (b is bool)
                    return (bool)a & (bool)b;
            }
            throw new Exception($"a ({a}) don't AND b ({b})");
        }

        private object OROpTwoObjects(object a, object b)
        {
            if (a is int)
            {
                if (b is int)
                    return (int)a | (int)b;
                if (b is long)
                    return (long)a | (long)b;
                if (b is byte)
                    return (int)a | (byte)b;
                if (b is char)
                    return (int)a | (char)b;
            }
            else if (a is long)
            {
                if (b is int)
                    return (long)a | (long)b;
                if (b is long)
                    return (long)a | (long)b;
                if (b is byte)
                    return (long)a | (byte)b;
                if (b is char)
                    return (long)a | (char)b;
            }
            else if (a is byte)
            {
                if (b is int)
                    return (byte)a | (int)b;
                if (b is long)
                    return (byte)a | (long)b;
                if (b is byte)
                    return (byte)a | (byte)b;
                if (b is char)
                    return (byte)a | (char)b;
            }
            else if (a is char)
            {
                if (b is int)
                    return (char)a | (int)b;
                if (b is long)
                    return (char)a | (long)b;
                if (b is byte)
                    return (char)a | (byte)b;
                if (b is char)
                    return (char)a | (char)b;
            }
            else if (a is bool)
            {
                if (b is bool)
                    return (bool)a | (bool)b;
            }
            throw new Exception($"a ({a}) don't AND b ({b})");
        }

        private object InvokeMember(object obj, string code, ref int startIndex, bool isStatic = false)
        {
            if (obj == null)
                throw new Exception($"Object reference not set to an instance of an object.");

            var token = GetToken(code, ref startIndex);
            var c = GetNextNonEmptyChar(code, ref startIndex);

            if (c == '=' && code[startIndex] != '=')
            {
                var v = ExecuteExpression(code, ref startIndex);
                SetMemberValue(token, obj.GetType().FullName, obj, v, isStatic);
                obj = v;
            }
            else if (c == '(')
            {
                Type type = GetObjType(obj, isStatic);
                var members = type.GetMember(token, MemberTypes.Method, flags);

                if (members.Length == 0)
                    throw new Exception($"Don't found a method ({token}) for matching parameter in type: {type.FullName}");

                var parameters = GetMethodParameters(code, ref startIndex);

                MethodInfo method = FindMethod(parameters, members);

                if (method != null)
                    obj = method.Invoke(method.IsStatic ? null : obj, parameters);
                else
                    throw new Exception($"Don't found a method ({token}) for matching parameter in type: {type.FullName}");

                c = GetNextNonEmptyChar(code, ref startIndex);
                if (c == '.')
                {
                    obj = InvokeMember(obj, code, ref startIndex, isStatic);
                }
            }
            else
            {
                obj = GetMemberValue(token, GetObjType(obj, isStatic).FullName, obj, isStatic);

                if (c == '.')
                {
                    obj = InvokeMember(obj, code, ref startIndex);
                }
                else if (c == '[')
                {
                    obj = ExecuteIList(obj, code, ref startIndex);
                }
            }

            return obj;
        }

        private MethodInfo FindMethod(object[] parameters, MemberInfo[] members)
        {
            for (int j = 0; j < members.Length; j++)
            {
                var m = members[j] as MethodInfo;
                var parameterInfos = m.GetParameters();
                if (parameters.Length == parameterInfos.Length && CheckParameters(parameters, parameterInfos))
                {
                    return m;
                }
            }

            return null;
        }

        private void SetMemberValue(string token, string typeName, object obj, object v, bool isStatic = false)
        {
            Type type = GetObjType(obj, isStatic);
            var field = type.GetField(token, flags);
            if (field != null)
                field.SetValue(field.IsStatic ? null : obj, v);
            else
            {
                var property = type.GetProperty(token, flags);
                if (property != null)
                {
                    var m = property.SetMethod;
                    if (m != null)
                        m.Invoke(m.IsStatic ? null : obj, new object[] { v });
                    else
                        throw new Exception($"There is no getmethod in property: {token}");
                }
                else
                    throw new Exception($"There is no member ({token}) in type: ({typeName})");
            }
        }

        private object GetMemberValue(string token, string typeName, object obj, bool isStatic = false)
        {
            Type type = GetObjType(obj, isStatic);
            var field = type.GetField(token, flags);
            //var field = GetField(type, token);
            if (field != null)
                obj = field.GetValue(field.IsStatic ? null : obj);
            else
            {
                var property = type.GetProperty(token, flags);
                if (property != null)
                {
                    var m = property.GetMethod;
                    if (m != null)
                        obj = m.Invoke(m.IsStatic ? null : obj, null);
                    else
                        throw new Exception($"There is no getmethod in property: {token}");
                }
                else
                    throw new Exception($"There is no member ({token}) in type: ({typeName})");
            }

            return obj;
        }

        private FieldInfo GetField(Type type, string token)
        {
            Type t = type;
            FieldInfo field = null;
            do
            {
                field = t.GetField(token, flags);
                t = t.BaseType;
            }
            while (field == null && t != null);

            return field;
        }

        private PropertyInfo GetProperty(Type type, string token)
        {
            Type t = type;
            PropertyInfo property = null;
            do
            {
                property = t.GetProperty(token, flags);
                t = t.BaseType;
            }
            while (property == null && t != null);

            return property;
        }

        private bool CheckParameters(object[] parameters, ParameterInfo[] parameterInfos)
        {
            bool matchParameter = true;
            for (int k = 0; k < parameterInfos.Length; k++)
            {
                var parameterType = parameterInfos[k].ParameterType;
                if (parameters[k] == null)
                {
                    if (parameterType.IsValueType)
                    {
                        matchParameter = false;
                        break;
                    }
                }
                else if (CheckConvert(parameters[k].GetType(), parameterType))
                {

                }
                else if (!parameterType.IsAssignableFrom(parameters[k].GetType()))
                {
                    matchParameter = false;
                    break;
                }
            }

            return matchParameter;
        }

        private bool CheckConvert(Type a, Type b)
        {
            if ( a == typeof(byte) || a == typeof(char))
            {
                if (b == typeof(int) || b == typeof(long) || b == typeof(float) || b == typeof(double))
                    return true;
            }
            else if ( a == typeof(int))
            {
                if (b == typeof(long) || b == typeof(float) || b == typeof(double))
                    return true;
            }
            else if (a == typeof(long))
            {
                if (b == typeof(float) || b == typeof(double))
                    return true;
            }
            else if (a == typeof(float))
            {
                if (b == typeof(double))
                    return true;
            }
            return false;
        }

        private bool IsInitialChar(char c)
        {
            return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_';
        }

        string GetToken(string code, ref int startIndex)
        {
            int i = startIndex;
            for (; i < code.Length; i++)
            {
                char c = code[i];

                if (i == startIndex && IsEmptyChar(c))
                {
                    startIndex = i + 1;
                }
                else if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_' || (startIndex != i && c >= '0' && c <= '9'))
                    continue;
                else
                    break;
            }

            var token = code.Substring(startIndex, i - startIndex);
            startIndex = i;
            return token;
        }

        private char GetNextNonEmptyChar(string code, ref int startIndex)
        {
            for (int i = startIndex; i < code.Length; i++)
            {
                char c = code[i];
                if (!IsEmptyChar(c))
                {
                    startIndex = i + 1;
                    return c;
                }
            }

            throw new Exception("Don't found non-empty Char");
        }

        private bool IsEmptyChar(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }

        object GetNumber(string code, ref int startIndex)
        {
            NumberType type = NumberType.Int32;
            NumberSystem numberSystem = NumberSystem.Decimal;
            int i = startIndex;
            for (; i < code.Length; i++)
            {
                char c = code[i];
                if (c == '0' && i + 1 < code.Length && code[i + 1] == 'x')
                {
                    numberSystem = NumberSystem.Hexadecimal;
                    i++;
                    startIndex = i + 1;
                }
                else if (c == '0' && i + 1 < code.Length && code[i + 1] == 'b')
                {
                    numberSystem = NumberSystem.Bianry;
                    i++;
                    startIndex = i + 1;
                }
                else if (c >= '0' && c <= '9')
                {

                }
                else if (c >= 'a' && c <= 'f' && numberSystem == NumberSystem.Hexadecimal)
                {

                }
                else if (c == '0' && c == '1' && numberSystem == NumberSystem.Bianry)
                {

                }
                else if (c == '.' && type == NumberType.Int32 && code[i + 1] >= '0' && code[i + 1] <= '9')
                {
                    type = NumberType.Double;
                }
                else if (c == 'f' && (type == NumberType.Int32 || type == NumberType.Double))
                {
                    string s = code.Substring(startIndex, i - startIndex);
                    startIndex = i + 1;
                    return Convert.ToSingle(s);
                }
                else if (c == 'l' && type == NumberType.Int32)
                {
                    string s = code.Substring(startIndex, i - startIndex);
                    startIndex = i + 1;
                    return Convert.ToInt64(s, (int)numberSystem);
                }
                else if (c == 'd' && (type == NumberType.Int32 || type == NumberType.Double))
                {
                    string s = code.Substring(startIndex, i - startIndex);
                    startIndex = i + 1;
                    return Convert.ToDouble(s);
                }
                else
                {
                    break;
                }
            }

            string s2 = code.Substring(startIndex, i - startIndex);
            startIndex = i;
            return Convert.ToInt32(s2, (int)numberSystem);
        }

        string GetString(string code, ref int startIndex)
        {
            int i = startIndex + 1;
            StringBuilder sb = new StringBuilder();
            for (; i < code.Length; i++)
            {
                char c = code[i];
                if (c == '\\' && i + 1 < code.Length)
                {
                    i++;
                    char c2 = code[i];
                    if (c2 == '\'')
                        sb.Append('\'');
                    else if (c2 == '\"')
                        sb.Append('\"');
                    else if (c2 == '\\')
                        sb.Append('\\');
                    else if (c2 == '0')
                        sb.Append('\0');
                    else if (c2 == 'a')
                        sb.Append('\a');
                    else if (c2 == 'b')
                        sb.Append('\b');
                    else if (c2 == 'f')
                        sb.Append('\f');
                    else if (c2 == 'n')
                        sb.Append('\n');
                    else if (c2 == 'r')
                        sb.Append('\r');
                    else if (c2 == 't')
                        sb.Append('\t');
                    else if (c2 == 'v')
                        sb.Append('\v');
                    else
                    {
                        sb.Append(c).Append(c2);
                    }
                }
                else if (c == '\"')
                {
                    i++;
                    break;
                }
                else
                    sb.Append(c);
            }

            startIndex = i;
            return sb.ToString();
        }

        char GetChar(string code, ref int startIndex)
        {
            int i = startIndex + 1;
            char? v = null;
            for (; i < code.Length; i++)
            {
                char c = code[i];
                if (IsEmptyChar(c))
                {
                    throw new Exception("char format is incorrect");
                }
                else if (c == '\\' && i + 1 < code.Length)
                {
                    if (v != null)
                        throw new Exception("char format is incorrect");

                    i++;
                    var c2 = code[i];
                    if (c2 == '\'')
                        v = '\'';
                    else if (c2 == '\"')
                        v = '\"';
                    else if (c2 == '\\')
                        v = '\\';
                    else if (c2 == '0')
                        v = '\0';
                    else if (c2 == 'a')
                        v = '\a';
                    else if (c2 == 'b')
                        v = '\b';
                    else if (c2 == 'f')
                        v = '\f';
                    else if (c2 == 'n')
                        v = '\n';
                    else if (c2 == 'r')
                        v = '\r';
                    else if (c2 == 't')
                        v = '\t';
                    else if (c2 == 'v')
                        v = '\v';
                    else
                    {
                        i--;
                        v = c;
                    }
                }
                else if (c == '\'')
                {
                    if (v == null)
                        throw new Exception("char format is incorrect");
                    startIndex = i + 1;
                    return v.Value;
                }
                else
                {
                    if (v != null)
                        throw new Exception("char format is incorrect");
                    v = c;
                }
            }

            throw new Exception("char format is incorrect");
        }

        private Type GetObjectType(string code, ref int startIndex)
        {
            var token = GetToken(code, ref startIndex);
            char c = GetNextNonEmptyChar(code, ref startIndex);
            Type type = null;
            if (c == '<')
            {
                var types = GetObjectTypes(code, ref startIndex);
                string genericTypeName = token + "`" + types.Length;
                Type genericType = GetObjType(genericTypeName);
                type = genericType.MakeGenericType(types);
            }
            else
            {
                type = GetObjType(token);

                if (type == null)
                {
                    if (c == '.')
                        type = GetObjectType(token, code, ref startIndex);
                    else
                        throw new Exception($"Don't found the type: ({token})");
                }
                else
                    startIndex--;
            }

            return type;
        }

        private Type[] GetObjectTypes(string code, ref int startIndex)
        {
            List<Type> types = new List<Type>();
            char c;
            do
            {
                var type = GetObjectType(code, ref startIndex);
                types.Add(type);
                c = GetNextNonEmptyChar(code, ref startIndex);
            }
            while (c == ',');

            /*if (types.Count == 0)
                throw new Exception($"There is no GenericParameter!");

            if (c != '>')
                throw new Exception("GenericType format is incorrect!");*/

            return types.ToArray();
        }

        private Type GetObjectType(string nameSpace, string code, ref int startIndex)
        {
            var token = GetToken(code, ref startIndex);
            token = nameSpace + '.' + token;

            var c = GetNextNonEmptyChar(code, ref startIndex);
            Type type = null;

            if (c == '<')
            {
                var types = GetObjectTypes(code, ref startIndex);
                string genericTypeName = token + "`" + types.Length;
                Type genericType = GetObjType(genericTypeName);
                type = genericType.MakeGenericType(types);
            }
            else
            {
                type = GetObjType(token);

                if (type == null)
                {
                    if (c == '.')
                        type = GetObjectType(token, code, ref startIndex);
                    else
                        throw new Exception($"Don't found the type: ({token})");
                }
                else
                    startIndex--;
            }

            return type;
        }

        private Type GetObjType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type == null)
            {
                var assemblys = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblys.Length; i++)
                {
                    type = assemblys[i].GetType(typeName);
                    if (type != null)
                        break;
                }
            }
            return type;
        }

        private Type GetObjType(object obj, bool isStatic = false)
        {
            if (isStatic && obj is Type type)
            {
                return type;
            }
            else
            {
                return obj.GetType();
            }
        }

        private object[] GetMethodParameters(string code, ref int startIndex)
        {
            var parameterList = new List<object>();
            char c = GetNextNonEmptyChar(code, ref startIndex);
            if (c != ')')
            {
                startIndex--;
                do
                {
                    var obj = ExecuteExpression(code, ref startIndex);
                    parameterList.Add(obj);
                    startIndex--;
                    c = GetNextNonEmptyChar(code, ref startIndex);
                }
                while (c == ',');
            }

            return parameterList.ToArray();
        }

        private MethodInfo GetDelegateMethod(string code, ref int startIndex)
        {
            int index = startIndex;
            string token = GetToken(code, ref startIndex);
            Type type;
            if (localVariableDics.TryGetValue(token, out var obj))
                type = obj.GetType();
            else
            {
                startIndex = index;
                type = GetObjectType(code, ref startIndex);
            }

            char c = GetNextNonEmptyChar(code, ref startIndex);
            if (c != '.')
                throw new Exception("Don't find method!");

            token = GetToken(code, ref startIndex);
            c = GetNextNonEmptyChar(code, ref startIndex);

            Type[] types = new Type[0];
            if (c == ',')
            {
                types = GetObjectTypes(code, ref startIndex);
            }

            var method = type.GetMethod(token, flags, null, CallingConventions.Any, types, null);

            if (method == null)
                throw new Exception("Don't find method!");

            return method;
        }

        private void SkipANDExpression(string code, ref int startIndex)
        {
            int layer = 0;
            for (int i = startIndex; i < code.Length; i++)
            {
                char c = code[i];
                if (c == '(')
                {
                    layer++;
                }
                else if (c == ')' && layer > 0)
                {
                    layer--;
                }
                else if (c == ';' && layer == 0)
                {
                    startIndex = i + 1;
                    return;
                }
                else if (c == ')' && layer == 0)
                {
                    startIndex = i + 1;
                    return;
                }
                else if (c == ']' && layer == 0)
                {
                    startIndex = i + 1;
                    return;
                }
                else if (c == '|' && code[i + 1] == '|' && layer == 0)
                {
                    startIndex = i + 1;
                    return;
                }
                else if (c == ',' && layer == 0)
                {
                    startIndex = i + 1;
                    return;
                }
            }

            throw new Exception("Don't Skip Expression!");
        }

        private void SkipORExpression(string code, ref int startIndex)
        {
            int layer = 0;
            for (int i = startIndex; i < code.Length; i++)
            {
                char c = code[i];
                if (c == '(')
                {
                    layer++;
                }
                else if (c == ')' && layer > 0)
                {
                    layer--;
                }
                else if (c == ';' && layer == 0)
                {
                    startIndex = i + 1;
                    return;
                }
                else if (c == ')' && layer == 0)
                {
                    startIndex = i + 1;
                    return;
                }
                else if (c == ']' && layer == 0)
                {
                    startIndex = i + 1;
                    return;
                }
                else if (c == ',' && layer == 0)
                {
                    startIndex = i + 1;
                    return;
                }
            }

            throw new Exception("Don't Skip Expression!");
        }

        private void SkipSingleExpression(string code, ref int startIndex)
        {
            for (int i = startIndex; i < code.Length; i++)
            {
                char c = code[i];
                if (c == ';')
                {
                    startIndex = i + 1;
                    return;
                }
            }

            throw new Exception("Don't Skip Expression!");
        }

        private void SkipClosure(string code, ref int startIndex)
        {
            int layer = 0;
            for (int i = startIndex; i < code.Length; i++)
            {
                char c = code[i];
                if (c == '{')
                    layer++;
                else if (c == '}' && layer > 0)
                    layer--;
                else if (c == '}' && layer == 0)
                {
                    startIndex = i + 1;
                    return;
                }
            }

            throw new Exception("Don't Skip Expression!");
        }

        private void SkipForThirdExpression(string code, ref int startIndex)
        {
            int layer = 0;
            for (int i = startIndex; i < code.Length; i++)
            {
                char c = code[i];
                if (c == '(')
                    layer++;
                else if (c == ')' && layer > 0)
                    layer--;
                else if (c == ')' && layer == 0)
                {
                    startIndex = i + 1;
                    return;
                }
            }

            throw new Exception("Don't Skip Expression!");
        }
    }
}
