open System
open System.Threading.Tasks
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open IcedTasks
open Falco.Markup
open Feliz.ViewEngine
open Giraffe.ViewEngine
open Hox
open Hox.Rendering
(*
* ToRun: dotnet run -c Release
* Initial Benchmarks: https://hamy.xyz/labs/2024-02_fsharp-html-dsl-long-page-benchmarks
* These benchmarks are somewhat non-representative as they are not comparing like-for-like. 
* Hox allows for async rendering and, the async mechanism overhead is not being accounted for in the other libraries.
* I may add cases for actual async work (e.g. reading from a sqlite database) and add actual
* async work, but in the meantime I want to be sure that any overhead is mostly
* due the async nature of Hox rather than my bad code in Hox itself.
*)

type TemplateProps = { Items: string array }

type HtmlDslLongPageBenchmarks() =
    let testItems: string[] = Array.init 5000 (fun _ -> Guid.NewGuid().ToString())

    let renderHox (props: TemplateProps) =
        valueTask {
            let node =
                h (
                    "html",
                    h (
                        "body",
                        h (
                            "table",
                            h ("tr", h ("th", "item")),
                            fragment (props.Items |> Array.map (fun i -> h ("tr", h ("td", i))))
                        )
                    )
                )

            return! Render.asString node
        }

    let renderHoxAsync (props: TemplateProps) =
        async {
            let node =
                h (
                    "html",
                    h (
                        "body",
                        h (
                            "table",
                            h ("tr", h ("th", "item")),
                            fragment (props.Items |> Array.map (fun i -> h ("tr", h ("td", i))))
                        )
                    )
                )

            return! Render.asStringAsync node
        }

    let renderHoxStream (stream: IO.Stream) (props: TemplateProps) =
        let node =
            h (
                "html",
                h (
                    "body",
                    h (
                        "table",
                        h ("tr", h ("th", "item")),
                        fragment (props.Items |> Array.map (fun i -> h ("tr", h ("td", i))))
                    )
                )
            )

        Render.toStream (node, stream)

    let renderHoxTaskSeq (props: TemplateProps) =
        let node =
            h (
                "html",
                h (
                    "body",
                    h (
                        "table",
                        h ("tr", h ("th", "item")),
                        fragment (props.Items |> Array.map (fun i -> h ("tr", h ("td", i))))
                    )
                )
            )

        Render.start node

    let renderGiraffeView (props: TemplateProps) : string =
        let page =
            html
                []
                [ body
                      []
                      [ table
                            []
                            [ tr [] [ th [] [ Text "item" ] ]
                              yield! (props.Items |> Array.map (fun i -> tr [] [ td [] [ Text i ] ])) ] ] ]

        page |> RenderView.AsString.htmlDocument

    let renderFalcoMarkup (props: TemplateProps) : string =
        let page =
            Elem.html
                []
                [ Elem.body
                      []
                      [ Elem.table
                            []
                            [ Elem.tr [] [ Elem.th [] [ Text.raw "item" ] ]
                              yield! (props.Items |> Array.map (fun i -> Elem.tr [] [ Elem.td [] [ Text.raw i ] ])) ] ] ]

        page |> renderNode

    let renderFelizViewEngine (props: TemplateProps) : string =
        let page =
            Html.html
                [ Html.body
                      [ Html.table
                            [ prop.children
                                  [ Html.tr [ Html.th [ prop.text "item" ] ]
                                    yield! (props.Items |> Array.map (fun i -> Html.tr [ Html.td [ prop.text i ] ])) ] ] ] ]

        page |> Render.htmlView

    [<Benchmark>]
    member __.RunGiraffeView() = renderGiraffeView { Items = testItems }

    [<Benchmark>]
    member __.RunFalcoMarkup() = renderFalcoMarkup { Items = testItems }

    [<Benchmark>]
    member __.RunFelizViewEngine() =
        renderFelizViewEngine { Items = testItems }

    [<Benchmark; BaselineColumn>]
    member __.RunHox() =
        (renderHox { Items = testItems }).AsTask()

    [<Benchmark>]
    member __.RunHoxFSharpAsync() =
        renderHoxAsync { Items = testItems } |> Async.StartImmediateAsTask

    [<Benchmark>]
    member __.RunHoxStream() =
        taskUnit {
            use stream = new IO.MemoryStream()
            return! renderHoxStream stream { Items = testItems }
        }

    [<Benchmark(Baseline = true)>]
    member __.RunHoxTaskSeq() = renderHoxTaskSeq { Items = testItems }


[<EntryPoint>]
let main argv =
    printfn "Hello World from F#!"

    let summary = BenchmarkRunner.Run<HtmlDslLongPageBenchmarks>()

    0 // return an integer exit code
