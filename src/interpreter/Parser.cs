﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace interpreter
{
    internal class Parser
    {
        private enum Associativity
        {
            Left,
            Right
        }
        
        private static readonly Dictionary<Token, int> precedence = new Dictionary<Token, int>();
        private static readonly Dictionary<Token, Associativity> associativity = new Dictionary<Token, Associativity>();

        static Parser()
        {
            precedence.Add(Token.LeftParenthesis, 0);
            precedence.Add(Token.RightParenthesis, 0);
            precedence.Add(Token.Equal, 1);
            precedence.Add(Token.Different, 1);
            precedence.Add(Token.Greater, 1);
            precedence.Add(Token.GreaterEqual, 1);
            precedence.Add(Token.Less, 1);
            precedence.Add(Token.LessEqual, 1);
            precedence.Add(Token.Addition, 2);
            precedence.Add(Token.Subtraction, 2);
            precedence.Add(Token.Multiplication, 3);
            precedence.Add(Token.Division, 3);
            precedence.Add(Token.Power, 4);

            associativity.Add(Token.Addition, Associativity.Left);
            associativity.Add(Token.Subtraction, Associativity.Left);
            associativity.Add(Token.Multiplication, Associativity.Left);
            associativity.Add(Token.Division, Associativity.Left);
            associativity.Add(Token.Power, Associativity.Right);
            associativity.Add(Token.Equal, Associativity.Left);
            associativity.Add(Token.Different, Associativity.Left);
            associativity.Add(Token.Greater, Associativity.Left);
            associativity.Add(Token.GreaterEqual, Associativity.Left);
            associativity.Add(Token.Less, Associativity.Left);
            associativity.Add(Token.LessEqual, Associativity.Left);
        }

        private Pattern GetPattern(Token token)
        {
            var memInfo = token.GetType().GetMember($"{token}");
            var attributes = memInfo.First().GetCustomAttributes(typeof(Pattern), false);
            return (Pattern)attributes.First();
        }

        private List<Symbol> Symbolize(string input)
        {
            var symbols = new List<Symbol>();
            var inputTrimmed = input.Trim();

            while (inputTrimmed.Any())
            {
                var previousCount = symbols.Count;

                foreach (Token token in Enum.GetValues(typeof(Token)))
                {
                    var pattern = GetPattern(token);
                    var m = pattern.Match(inputTrimmed);

                    if (m.Success)
                    {
                        symbols.Add(new Symbol(m.Value, token));
                        inputTrimmed = inputTrimmed.Substring(m.Value.Length);

                        break;
                    }
                }

                if (previousCount == symbols.Count)
                {
                    var position = input.Length - inputTrimmed.Length + 1;
                    throw new ParserException(position, input);
                }

                inputTrimmed = inputTrimmed.Trim();
            }

            return symbols;
        }

        private bool HasHigherPrecedenceOrIsRight(Stack<Symbol> stack, Symbol symbol)
        {
            var token = symbol.Token;
            var topToken = stack.Peek().Token;

            var isHigher = precedence[token] > precedence[topToken];
            var isEqual = precedence[token] == precedence[topToken];
            var isRight = associativity[token] == Associativity.Right;

            return isHigher || (isEqual && isRight);
        }

        private void Shunt(Stack<Symbol> operators, Stack<Symbol> output, Symbol symbol)
        {
            var token = symbol.Token;

            if (token == Token.Number)
            {
                // Rule 1: If the incoming symbol is operand, output it.
                output.Push(symbol);
            }
            else if (token == Token.LeftParenthesis)
            {
                // Rule 2: If the incoming symbol is left parenthesis, push it
                // on the stack.
                operators.Push(symbol);
            }
            else if (token == Token.RightParenthesis)
            {
                // Rule 3: If the incoming symbol is right parenthesis, discart
                // it and output the stack until you see an left parenthesis,
                // then discart it as well.
                while (operators.Peek().Token != Token.LeftParenthesis)
                {
                    output.Push(operators.Pop());
                }

                operators.Pop();
            }
            else if (!operators.Any() || operators.Peek().Token == Token.LeftParenthesis)
            {
                // Rule 4: If the stack is empty or the top of the stack is
                // left parenthesis, push the incoming symbol on the stack.
                operators.Push(symbol);
            }
            else if (HasHigherPrecedenceOrIsRight(operators, symbol))
            {
                // Rule 5: If the incoming symbol has either higher precedence
                // than the operator on top of the stack or has the same 
                // precedence and is right associative, push in on the stack.
                operators.Push(symbol);
            }
            else
            {
                // Rule 6: Output the stack until rule 5 gets true and then
                // push the incoming symbol on the stack.
                while (operators.Any() && !HasHigherPrecedenceOrIsRight(operators, symbol))
                {
                    output.Push(operators.Pop());
                }

                operators.Push(symbol);
            }
        }

        internal IEnumerable<Symbol> Parse(string input)
        {
            var operators = new Stack<Symbol>();
            var output = new Stack<Symbol>();

            foreach (var symbol in Symbolize(input))
            {
                Shunt(operators, output, symbol);
            }

            return output.Reverse().Concat(operators);
        }
    }
}
