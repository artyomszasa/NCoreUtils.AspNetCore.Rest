module internal NCoreUtils.AspNetCore.Rest.Emitter

open System
open System.Reflection
open System.Reflection.Emit

[<Literal>]
let private DataFieldName = "__generated_data"

[<Literal>]
let private GetterName = "get_Data"

[<Literal>]
let private SetterName = "set_Data"

[<Literal>]
let private PropertyName = "Data"

[<Literal>]
let private AssemblyName = "NCoreUtils.AspNetCore.Rest.Generated"

[<Literal>]
let private MethodAttrs = MethodAttributes.Public ||| MethodAttributes.SpecialName ||| MethodAttributes.HideBySig ||| MethodAttributes.Virtual

let private sync = obj ()

let mutable private moduleBuilder = null
let private emitDataField (typeBuilder : TypeBuilder) =
  typeBuilder.DefineField (DataFieldName, typeof<byte[]>, FieldAttributes.Private)

let private emitDataProperty (typeBuilder : TypeBuilder) (field : FieldInfo) =
  let getterBuilder =
    let getter = typeBuilder.DefineMethod (GetterName, MethodAttrs, typeof<byte[]>, Type.EmptyTypes)
    let il = getter.GetILGenerator ()
    il.Emit OpCodes.Ldarg_0
    il.Emit (OpCodes.Ldfld, field)
    il.Emit OpCodes.Ret
    getter
  let setterBuilder =
    let setter = typeBuilder.DefineMethod (SetterName, MethodAttrs, typeof<System.Void>, [| typeof<byte[]> |])
    let il = setter.GetILGenerator ()
    il.Emit OpCodes.Ldarg_0
    il.Emit OpCodes.Ldarg_1
    il.Emit (OpCodes.Stfld, field)
    il.Emit OpCodes.Ret
    setter
  let propertyBuilder = typeBuilder.DefineProperty(PropertyName, PropertyAttributes.None, typeof<byte[]>, Type.EmptyTypes)
  propertyBuilder.SetGetMethod getterBuilder
  propertyBuilder.SetSetMethod setterBuilder
  propertyBuilder

let private emitType (baseType : Type) (moduleBuilder : ModuleBuilder) =
  let typeBuilder = moduleBuilder.DefineType (sprintf "%s_Upload" baseType.Name, TypeAttributes.Public ||| TypeAttributes.Sealed, baseType, [| typeof<IHasUploadData> |])
  emitDataField typeBuilder |> emitDataProperty typeBuilder |> ignore
  let typeInfo = typeBuilder.CreateTypeInfo ()
  typeInfo.AsType ()

let emitDataUploadType (baseType : Type) =
  if isNull moduleBuilder then
    lock sync
      (fun () ->
        if isNull moduleBuilder then
          let assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly (System.Reflection.AssemblyName AssemblyName, AssemblyBuilderAccess.Run)
          moduleBuilder <- assemblyBuilder.DefineDynamicModule AssemblyName
      )
  emitType baseType moduleBuilder