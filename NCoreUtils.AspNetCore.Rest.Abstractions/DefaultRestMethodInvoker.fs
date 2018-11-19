namespace NCoreUtils.AspNetCore.Rest

type DefaultRestMethodInvoker =
  new () = { }
  interface IRestMethodInvoker with
    member __.AsyncInvoke<'TResult> (target : RestMethodInvocation<'TResult>) =
        match target.Instance with
        | :? IRestTrasactedMethod as txMethod ->
          async {
            use! tx = txMethod.AsyncBeginTransaction ()
            let! res = target.AsyncInvoke ()
            tx.Commit ()
            return res }
        | _ -> target.AsyncInvoke ()