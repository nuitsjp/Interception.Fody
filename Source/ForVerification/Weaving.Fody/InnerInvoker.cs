using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Weaving.Fody
{
    public class InnerInvoker
    {
        public TypeDefinition TypeDefinition { get; }

        public FieldDefinition ParentTypeFieldDefinition { get; private set; }

        public List<FieldDefinition> ParameterFieldDefinisions { get; } = new List<FieldDefinition>();

        public InnerInvoker(TypeDefinition typeDefinition)
        {
            TypeDefinition = typeDefinition;
        }

        public static  InnerInvoker Create(ModuleDefinition moduleDefinition, TypeDefinition parent, MethodDefinition targetMethod)
        {
            var innerInvoker = new TypeDefinition(parent.Namespace, $"{targetMethod.Name}InnerInvoker", TypeAttributes.NotPublic | TypeAttributes.NestedPrivate);
            var result = new InnerInvoker(innerInvoker);

            var invocationType = moduleDefinition.ImportReference(typeof(Invocation));
            innerInvoker.BaseType = invocationType;


            // Constructor
            var methodAttributes = MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                                   MethodAttributes.RTSpecialName;
            var constructor = new MethodDefinition(".ctor", methodAttributes, moduleDefinition.TypeSystem.Void);
            constructor.Parameters.Add(new ParameterDefinition("interceptorTypes", ParameterAttributes.None, moduleDefinition.ImportReference(typeof(Type).MakeArrayType())));
            constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));

            var baseConstructor = invocationType.Resolve().GetConstructors().Single();
            constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, moduleDefinition.ImportReference(baseConstructor)));
            constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            // Fields
            var parentField = new FieldDefinition(parent.Name, FieldAttributes.Assembly, parent);
            innerInvoker.Fields.Add(parentField);
            result.ParentTypeFieldDefinition = parentField;

            foreach (var (parameter, index) in targetMethod.Parameters.Select((parameter, index) => (parameter, index)))
            {
                var parameterFieldDefinition =
                    new FieldDefinition($"Value{index + 1}", FieldAttributes.Assembly, parameter.ParameterType);
                innerInvoker.Fields.Add(parameterFieldDefinition);
                result.ParameterFieldDefinisions.Add(parameterFieldDefinition);
            }

            // Properties
            var arguments = new PropertyDefinition("Arguments", PropertyAttributes.None, moduleDefinition.ImportReference(typeof(object).MakeArrayType())) { HasThis = true };

            var getArgumentsAttributes =
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                MethodAttributes.Virtual;
            var getArguments = new MethodDefinition("get_Arguments", getArgumentsAttributes, moduleDefinition.ImportReference(typeof(object).MakeArrayType())) { HasThis = true };
            getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, targetMethod.Parameters.Count));
            getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, moduleDefinition.TypeSystem.Object));

            foreach (var (fieldDefinition, index) in result.ParameterFieldDefinisions.Select((fieldDefinition, index) => (fieldDefinition, index)))
            {
                getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
                getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, index));
                getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, fieldDefinition));
                getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Box, moduleDefinition.TypeSystem.Int32));
                getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
            }

            getArguments.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            innerInvoker.Methods.Add(getArguments);

            arguments.GetMethod = getArguments;
            innerInvoker.Properties.Add(arguments);

            // InvokeEndpoint
            var invokeEndpointAttribute = MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual;
            var invokeEndpoint = new MethodDefinition("InvokeEndpoint", invokeEndpointAttribute, moduleDefinition.TypeSystem.Object);
            invokeEndpoint.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            invokeEndpoint.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, parentField));
            foreach (var valueFieldDefinition in result.ParameterFieldDefinisions)
            {
                invokeEndpoint.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                invokeEndpoint.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, valueFieldDefinition));
            }
            invokeEndpoint.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, targetMethod));
            invokeEndpoint.Body.Instructions.Add(Instruction.Create(OpCodes.Box, moduleDefinition.TypeSystem.Int32));
            invokeEndpoint.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            innerInvoker.Methods.Add(invokeEndpoint);


            innerInvoker.Methods.Add(constructor);

            return result;
        }
    }
}
