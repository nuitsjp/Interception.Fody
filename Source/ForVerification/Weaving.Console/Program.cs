using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Weaving.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = @"..\..\..\WeaveTarget.Console\bin\Debug\WeaveTarget.dll";
            byte[] assemblyImage;
            using (var stream = new FileStream(path, FileMode.Open))
            {
                assemblyImage = new byte[stream.Length];
                stream.Read(assemblyImage, 0, assemblyImage.Length);
            }

            using (var stream = new MemoryStream(assemblyImage))
            {
                var module = ModuleDefinition.ReadModule(stream);

                var weaver = new Weaver();
                weaver.Weave(module);
                //var type = module.Types.Single(x => x.Name == "Class1");

                //var addMethod = type.Methods.Single(x => x.Name == "Add");
                //var addInnerMethod = type.Methods.Single(x => x.Name == "AddInner");
                //addMethod.Name = "AddInner";
                //addInnerMethod.Name = "Add";

                //var newMethod = CopyMethod(module, type, addMethod);

                //newMethod.Body.MaxStackSize = addMethod.Parameters.Count + 1;

                //newMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));

                //newMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                //newMethod.Parameters.ToList().ForEach(x => addMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_S, x)));

                //addMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, newMethod));
                //addMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc_0));

                //var ldlock0 = Instruction.Create(OpCodes.Ldloc_0);
                //addMethod.Body.Instructions.Add(ldlock0);

                //addMethod.Body.GetILProcessor().InsertBefore(ldlock0, Instruction.Create(OpCodes.Br_S, ldlock0));
                //addMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

                module.Write(path);
            }
        }

        private static MethodDefinition CopyMethod(ModuleDefinition module, TypeDefinition type, MethodDefinition sourceMethod)
        {
            var targetMethod =
                new MethodDefinition(sourceMethod.Name + "Inner", MethodAttributes.Private | MethodAttributes.HideBySig, type)
                {
                    Body =
                    {
                        MaxStackSize = sourceMethod.Body.MaxStackSize,
                        InitLocals = sourceMethod.Body.InitLocals
                    },
                    ReturnType = sourceMethod.ReturnType
                };

            sourceMethod.Parameters.ToList().ForEach(x => targetMethod.Parameters.Add(x));
            sourceMethod.Body.Variables.ToList().ForEach(x => targetMethod.Body.Variables.Add(x));
            sourceMethod.Body.Instructions.ToList().ForEach(x => targetMethod.Body.Instructions.Add(x));

            type.Methods.Add(targetMethod);
            return targetMethod;
        }
    }

    public interface IInvocation
    {
        object[] Arguments { get; set; }

        object Proceed();
    }

    public abstract class Invocation : IInvocation
    {
        public object[] Arguments { get; set; }

        public abstract object Proceed();
    }

    public interface IInterceptor
    {
        void Intercept(IInvocation invocation);
    }

    public class ConcreteInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            invocation.Proceed();
        }
    }

}
