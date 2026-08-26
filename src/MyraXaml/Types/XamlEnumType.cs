using Myra.Xaml.Helpers;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
#if !XAMLX_INTERNAL
    public
#endif
    class XamlEnumNode :
        XamlAstNode,
        IXamlAstValueNode,
        IXamlAstEmitableNode<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public IXamlField Value { get; }

        public XamlEnumNode(IXamlLineInfo lineInfo, IXamlType clrType, IXamlField value)
            : base(lineInfo)
        { 
            if (!clrType.IsEnum)
                throw new ArgumentException(
                    $"Type '{clrType}' is not an enum.",
                    nameof(clrType));

            if (value == null)
                throw new ArgumentException(
                    $"Value must be an instance of '{clrType}'.",
                    nameof(value));

            Value = value;
            Type = new XamlAstClrTypeReference(lineInfo, clrType, false);
        }

        public IXamlAstTypeReference Type { get; }

        public XamlILNodeEmitResult Emit(
            XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlILEmitter codeGen)
        {
            var constant = Value.GetLiteralValue();
            var underlyingType = Type.GetClrType().GetEnumUnderlyingType();
            var returnValue = XamlILNodeEmitResult.Type(0, Type.GetClrType()); 


            if (underlyingType == context.Configuration.WellKnownTypes.Int32
                || underlyingType == TypesContainer.Int16)
            {
                codeGen.Ldc_I4(Convert.ToInt32(constant));
                return returnValue;
            }

            if ( underlyingType == TypesContainer.Byte
                || underlyingType == TypesContainer.UInt16
                || underlyingType == TypesContainer.UInt32)
            {
                codeGen.Ldc_I4(unchecked((int)Convert.ToUInt32(constant)));
                return returnValue;
            }


            if (underlyingType == TypesContainer.Int64)
            {
                codeGen.Emit(
                        OpCodes.Ldc_I8,
                        Convert.ToInt64(constant));
                return returnValue;

            }

            if (underlyingType == TypesContainer.UInt64)
            {
                codeGen.Emit(
                        OpCodes.Ldc_I8,
                        unchecked((long)Convert.ToUInt64(constant)));
                return returnValue; 
            }

            throw new NotSupportedException($"Enum type '{underlyingType}' is not supported!");
        }
    }
}
