using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication2
{
    class Program
    {
        delegate void _delegate(Mono.Cecil.TypeDefinition type,
                                Mono.Cecil.AssemblyDefinition assembly,
                                string prefix);

        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                Console.WriteLine("実行開始");
                var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(args[0]);
                assembly.Modules
                    .SelectMany(x => x.Types)
                    .ToList()
                    .ForEach((type) =>
                    {
                        showString(type, assembly);
                        debugMethod(type, assembly);
                    });

                var dirName = System.IO.Path.GetDirectoryName(args[0]);
                var fileName = System.IO.Path.GetFileName(args[0]);

                if (dirName == "")
                {
                    assembly.Write("change_" + args[0]);
                }
                else
                {
                    assembly.Write(dirName + "\\change_" + fileName);
                }
               
            }
            else
            {
                Console.WriteLine("引き数にdllかexeを指定してください。");
            }
        }

        static private void showString(Mono.Cecil.TypeDefinition type,
                                       Mono.Cecil.AssemblyDefinition assembly,
                                       string prefix = "")
        {
            nestSearch(type, assembly, ref prefix, showString);

            type.Methods.ToList().ForEach((method) =>
            {
                var lineCount = 0;
                if (method.HasBody)
                {
                    method.Body.Instructions.ToList().ForEach(inst =>
                    {
                        if (inst.OpCode == Mono.Cecil.Cil.OpCodes.Ldstr)
                        {
                            if (lineCount == 0)
                            {
                                Console.WriteLine(string.Format("{0}.{1}:", prefix, method.Name));
                            }
                            Console.WriteLine(string.Format("{0}:{1}", lineCount, inst.Operand.ToString()));
                            lineCount++;
                        }
                    });
                }

                if (lineCount != 0)
                {
                    Console.WriteLine();
                }
            });
        }

        static private void debugMethod(Mono.Cecil.TypeDefinition type,
                                       Mono.Cecil.AssemblyDefinition assembly,
                                       string prefix = "")
        {
            nestSearch(type, assembly, ref prefix, debugMethod);

            type.Methods.ToList().ForEach((method) =>
            {
                var il = method.Body.GetILProcessor();
                var module = type.Module;
                var write = il.Create(
                    Mono.Cecil.Cil.OpCodes.Call,
                    module.Import(typeof(Console).GetMethod("WriteLine", new[] { typeof(object) })));
                var ret = il.Create(Mono.Cecil.Cil.OpCodes.Ret);
                var leave = il.Create(Mono.Cecil.Cil.OpCodes.Leave, ret);

                il.InsertAfter(
                    method.Body.Instructions.Last(),
                    write);

                il.InsertAfter(write, leave);
                il.InsertAfter(leave, ret);

                var handler = new Mono.Cecil.Cil.ExceptionHandler(Mono.Cecil.Cil.ExceptionHandlerType.Catch)
                {
                    TryStart = method.Body.Instructions.First(),
                    TryEnd = write,
                    HandlerStart = write,
                    HandlerEnd = ret,
                    CatchType = module.Import(typeof(Exception)),
                };

                method.Body.ExceptionHandlers.Add(handler);
            });
        }

        static private void nestSearch(Mono.Cecil.TypeDefinition type,
                                       Mono.Cecil.AssemblyDefinition assembly,
                                       ref string prefix,
                                       _delegate del)
        {
            if (prefix != "")
            {
                prefix += ".";
            }
            if (type.Namespace != "")
            {
                prefix += type.Namespace + ".";
            }
            prefix += type.Name;

            var _prefix = prefix;
            type.NestedTypes.ToList().ForEach(x =>
            {
                del(x, assembly, _prefix);
            });
        }
    }
}