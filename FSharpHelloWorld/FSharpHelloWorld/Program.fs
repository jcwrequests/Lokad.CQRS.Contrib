// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
open System

[<EntryPoint>]
let main argv = 
    printfn "%A" argv
   
    let add x y = 
        x + y

    Console.WriteLine(add 2 2)
    
    let sub x y =
        x - y

    let console = Seq.initInfinite (fun _ -> Console.ReadLine())

    let items = [1..1000]

    let sumItem = Seq.sum(items)

    let avg = Seq.average(items items.Length)


    printfn "sum %d" (sumItem)

    items |> Seq.iter (fun item ->
                                printfn "1 + %d = %d" item (add (1)(item))
                                printfn "10 - %d = %d" item (sub 10 item)
                      )

    console |> Seq.iter (fun line ->
                                    printfn "You said %s" line
                                    printfn "1 + 2 = %d" (add 1 2)
                                    printfn "2 - 1 = %d" (sub 2 1)

                         )
   



    0 // return an integer exit code