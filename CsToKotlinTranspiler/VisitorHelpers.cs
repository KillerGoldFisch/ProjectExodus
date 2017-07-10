﻿// -----------------------------------------------------------------------
//   <copyright file="VisitorHelpers.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CsToKotlinTranspiler
{
    public partial class KotlinTranspilerVisitor
    {
        private readonly StringBuilder _sb = new StringBuilder();
        
        private string GetKotlinType(TypeSyntax type)
        {
            if (type is ArrayTypeSyntax arr)
            {
                var t = GetKotlinType(arr.ElementType);
                return $"arrayOf<{t}>";
            }

            var si = _model.GetSymbolInfo(type);
            var s = si.Symbol;
            if (s == null)
            {
                Debugger.Break();
                var d = _model.GetDeclaredSymbol(type);
                var ti = _model.GetTypeInfo(type);
                if (ti.Type?.Kind == SymbolKind.ErrorType)
                {
                    return "**error type**";
                }

                return "**unknown type**"; //TODO: how does this work?
            }
            return GetKotlinType(s as ITypeSymbol);
        }

        private string GetKotlinType(ITypeSymbol s)
        {
            var named = s as INamedTypeSymbol;
            if (named != null)
            {
                if (s.TypeKind == TypeKind.Delegate)
                {
                    var args = named.DelegateInvokeMethod.Parameters.Select(p => p.Type).Select(GetKotlinType);
                    var ret = GetKotlinType(named.DelegateInvokeMethod.ReturnType);
                    return $"({string.Join(", ", args)}) -> {ret}";
                }

                if (named?.IsGenericType == true)
                {
                    var name = GetGenericName(named.Name);

                    var args = named.TypeArguments.Select(GetKotlinType);
                    return $"{name}<{string.Join(", ", args)}>";
                }
            }

            if (s.Kind == SymbolKind.ArrayType)
            {
                var arr = (IArrayTypeSymbol) s;
                return $"Array<{GetKotlinType(arr.ElementType)}>";
            }
            return GetName(s.Name);
        }

        private string GetName(string name) 
        {
            switch (name)
            {
                case "Void":
                    return "Unit";
                case "TimeSpan":
                    return "Duration";
                case "Object":
                    return "Any";
                case "Int32":
                    return "Int";
                case "Boolean":
                    return "Boolean";
                case "String":
                    return "String";
                case "ArgumentException":
                    return "IllegalArgumentException";
                default:
                    return name;
            }
        }

        private static string GetGenericName(string name)
        {
            switch (name)
            {
                case "ConcurrentQueue":
                    return "ConcurrentLinkedQueue";
                case "ConcurrentDictionary":
                    return "ConcurrentHashMap";
                case "List":
                    return "MutableList";
                case "Set":
                    return "MutableSet";
                case "Stack":
                    return "Stack";
                default:
                    return name;
            }
        }

        public string GetKotlinPackageName(string ns)
        {
            return ns.ToLowerInvariant();
        }

        private string GetArgList(ParameterListSyntax node)
        {
            List<string> GetArgumentList(ParameterListSyntax parameterList)
            {
                return parameterList.Parameters.Select(p =>
                {
                    if (p.Type == null)
                    {
                        return p.Identifier.ToString();
                    }

                    return p.Identifier + " : " + GetKotlinType(p.Type);
                }).ToList();
            }

            var arg = string.Join(", ", GetArgumentList(node));
            return arg;
        }

        private void IndentWrite(string text)
        {
            Write(GetIndent() + text);
        }

        private void Indent()
        {
            Write(GetIndent());
        }

        private void Write(string text)
        {
            Console.Write(text);
            _sb.Append(text);
        }

        private void NewLine()
        {
            Write("\n");
        }

        private void IndentWriteLine(string text)
        {
            Write(GetIndent() + text);
            NewLine();
        }

        private string GetIndent()
        {
            return new string(' ', _indent * 4);
        }

        private static string ToCamelCase(string name)
        {
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        private static bool FieldIsReadOnly(FieldDeclarationSyntax node)
        {
            return node.Modifiers.Any(m => m.Text == "readonly" || m.Text=="const");
        }

        private bool IsInterfaceMethod(MethodDeclarationSyntax node)
        {
            var methodSymbol = _model.GetDeclaredSymbol(node);
            bool isInterfaceMethod = methodSymbol.ContainingType
                                                 .AllInterfaces
                                                 .SelectMany(@interface => @interface.GetMembers().OfType<IMethodSymbol>())
                                                 .Any(method => methodSymbol.Equals(methodSymbol.ContainingType.FindImplementationForInterfaceMember(method)));
            return isInterfaceMethod;
        }

        private bool IsInterfaceProperty(PropertyDeclarationSyntax node)
        {
            var methodSymbol = _model.GetDeclaredSymbol(node);
            bool isInterfaceMethod = methodSymbol.ContainingType
                                                 .AllInterfaces
                                                 .SelectMany(@interface => @interface.GetMembers().OfType<IPropertySymbol>())
                                                 .Any(method => methodSymbol.Equals(methodSymbol.ContainingType.FindImplementationForInterfaceMember(method)));
            return isInterfaceMethod;
        }

        private string GetKotlinDefaultValue(TypeSyntax type)
        {
            var si = _model.GetSymbolInfo(type);
            var s = si.Symbol;
            if (s == null)
            {
                return null;
            }
            var str = s.Name;
            switch (str)
            {
                case "Int64":
                    return "0";
                case "Int32":
                    return "0";
                case "Boolean":
                    return "false";
                case "String":
                    return "\"\"";
            }
            if (s is INamedTypeSymbol named)
            {
                if (named.TypeKind == TypeKind.Enum)
                {
                    return $"{named.Name}.{named.MemberNames.First()}";
                }

                if (named.TypeKind == TypeKind.Struct)
                {
                    var t = GetKotlinType(type);
                    return $"{t}()"; //structs are initialized to empty ctor
                }
            }

            
            return null;
        }

        public string Run(SyntaxNode root)
        {
            Visit(root);
            return _sb.ToString();
        }
    }
}