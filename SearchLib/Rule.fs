﻿namespace SearchLib

open SearchLib.Argument
open SearchLib.Signature

module Rule =
    type predicate = F0 of result | F1 of ((argument) -> result) | F2 of (argument -> argument -> result) | F3 of (argument -> argument -> argument -> result)
    type rule = Rule of signature * f: predicate | ConcatenatedRule of signature * signature list
        with
        member r.Name = 
            match r with
            | Rule(s, b) -> s.name
            | ConcatenatedRule(s, calls) -> s.name
        member r.Parameters = 
            match r with
            | Rule(s, b) -> s.parameters
            | ConcatenatedRule(s, calls) -> s.parameters
        member r.Signature = 
            match r with
            | Rule(s, b) -> s
            | ConcatenatedRule(s, calls) -> s
        static member fact(s: signature) = 
            match s.parameters with
            | [] -> Rule(s, F0(Accepted([])))
            | h::[] -> Rule(s, F1(fun arg -> if arg ?= h then Accepted([h]) else Rejected))
            | h1::h2::[] -> Rule(s, F2(fun arg1 -> fun arg2 -> if arg1 ?= h1 && arg2 ?= h2 then Accepted([h1;h2]) else Rejected))
            | h1::h2::h3::[] -> Rule(s, F3(fun arg1 -> fun arg2 -> fun arg3 -> if arg1 ?= h1 && arg2 ?= h2 && arg3 ?= h3 then Accepted([h1;h2;h3]) else Rejected))
            | _ -> failwith("Too many arguments for instantiating a fact.")
        static member create(name: string) (prms: parameter list) (p: predicate) = Rule({name = name; parameters = prms}, p)
    type rulelist = list<rule>
    
    let Fact(name: string) (prms: parameter list) = rule.fact {name = name; parameters = prms}
    
    let Rule(name: string) (prms: parameter list) (p: predicate) = rule.create name prms p
    
    let ConRule(name: string) (prms: parameter list) (calls: signature list) = rule.ConcatenatedRule(call name prms, calls)

    let IncR: rule = 
        let inc(p1: argument) (p2: argument):result =
            let v1 = asInt p1
            let v2 = asInt p2
            match (v1, v2) with
                | (Some(val1), Some(val2)) -> if val1 + 1 = val2 then Accepted([p1;p2]) else Rejected
                | _ -> Rejected
        Rule "inc" ["A";"B"] (F2 inc)

    let SumR: rule = 
        let sum(p1: argument) (p2: argument) (p3: argument):result =
            let v1 = asInt p1
            let v2 = asInt p2
            let v3 = asInt p3
            match (v1, v2, v3) with
                | (Some(val1), Some(val2), Some(val3)) -> if val1 + val2 = val3 then Accepted([p1;p2;p3]) else Rejected
                | (Some(val1), Some(val2), None) -> Accepted([p1;p2;to_arg(val1 + val2)])
                | (Some(val1), None, Some(val3)) -> Accepted([p1;to_arg(val3 - val1);p3])
                | (None, Some(val2), Some(val3)) -> Accepted([to_arg(val3 - val2);p2;p3])
                | _ -> Rejected
        Rule "sum" ["A";"B";"C"] (F3 sum)

    let DivsR:rule = 
        let divisors(p1: argument) (p2: argument): result =
            let v1 = asInt p1
            let v2 = asIntList p2
            let getdivs x = {1..x} |> Seq.filter(fun i -> x % i = 0) |> Set.ofSeq
            match (v1, v2) with
            | (Some(val1), Some(val2)) -> if Set.isEmpty(Set.difference (getdivs val1) (Set.ofList val2)) then Accepted([p1; p2]) else Rejected
            | (Some(val1), None) -> Accepted([p1; getdivs val1 |> Set.toList |> List.fold(fun acc x -> acc + x.ToString() + " ") ""])
            | (None, Some(val2)) -> Rejected
            | _ -> Rejected
        Rule "divs" ["A";"B"] (F2 divisors)

    let defaultRules : rulelist = [IncR; SumR; DivsR]