using System;
using System.Linq;
using System.Text.RegularExpressions;
using static Neo.BuildTasks.CSharpHelpers;

namespace Neo.BuildTasks
{
    public static class ContractGenerator
    {
        public static string GenerateContractInterface(NeoManifest manifest, string contractNameOverride, string @namespace)
        {
            var contractName = string.IsNullOrEmpty(contractNameOverride)
                ? Regex.Replace(manifest.Name, "^.*\\.", string.Empty)
                : contractNameOverride;

            if (!IsValidTypeName(contractName) || contractName.Contains('.'))
            {
                throw new Exception($"\"{contractName}\" is not a valid C# type name");
            }

            var builder = new IndentedStringBuilder();

            builder.AppendLines($@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

");

            if (@namespace.Length > 0)
            {
                builder.AppendLine($"namespace {@namespace} {{");
                builder.IncrementIndent();
            }
            builder.AppendLines($@"#if NETSTANDARD || NETFRAMEWORK || NETCOREAPP
[System.CodeDom.Compiler.GeneratedCode(""Neo.BuildTasks"",""{ThisAssembly.AssemblyFileVersion}"")]
#endif
");
            builder.AppendLine($"[System.ComponentModel.Description(\"{manifest.Name}\")]");

            builder.AppendLine($"interface {contractName} {{");
            builder.IncrementIndent();
            for (int i = 0; i < manifest.Methods.Count; i++)
            {
                var method = manifest.Methods[i];
                if (method.Name.StartsWith("_")) continue;

                builder.Append($"{ConvertParameterType(method.ReturnType)} {method.Name}(");
                builder.Append(string.Join(", ", method.Parameters.Select(p => $"{ConvertParameterType(p.Type)} {CreateEscapedIdentifier(p.Name)}")));
                builder.AppendLine(");");
            }

            if (manifest.Events.Count > 0)
            {
                builder.AppendLine("interface Events {");
                builder.IncrementIndent();
                for (int i = 0; i < manifest.Events.Count; i++)
                {
                    var @event = manifest.Events[i];
                    builder.Append($"void {@event.Name}(");
                    builder.Append(string.Join(", ", @event.Parameters.Select(p => $"{ConvertParameterType(p.Type)} {CreateEscapedIdentifier(p.Name)}")));
                    builder.AppendLine($");");
                }
                builder.DecrementIndent();
                builder.AppendLine("}");
            }

            builder.DecrementIndent();
            builder.AppendLine("}");

            if (@namespace.Length > 0)
            {
                builder.DecrementIndent();
                builder.AppendLine("}");
            }

            return builder.ToString();

            static string ConvertParameterType(string parameterType) => parameterType switch
            {
                "Any" => "object",
                "Array" => "Neo.VM.Types.Array",
                "Boolean" => "bool",
                "ByteArray" => "byte[]",
                "Hash160" => "Neo.UInt160",
                "Hash256" => "Neo.UInt256",
                "Integer" => "System.Numerics.BigInteger",
                "InteropInterface" => "Neo.VM.Types.InteropInterface",
                "PublicKey" => "Neo.Cryptography.ECC.ECPoint",
                "Map" => "Neo.VM.Types.Map",
                "Signature" => "Neo.VM.Types.ByteString",
                "String" => "string",
                "Void" => "void",
                _ => throw new FormatException($"Invalid parameter type {parameterType}")
            };
        }
    }
}
