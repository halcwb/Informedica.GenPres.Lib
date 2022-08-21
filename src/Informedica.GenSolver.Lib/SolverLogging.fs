namespace Informedica.GenSolver.Lib


module SolverLogging =

    open Informedica.Utils.Lib.BCL

    open Types
    open Types.Logging
    open Types.Events

    module Name = Variable.Name
    module ValueRange = Variable.ValueRange


    let private eqsToStr eqs =
        let eqs =
            eqs
            |> List.sortBy (fun e ->
                e
                |> Equation.toVars
                |> List.tryHead
                |> function
                | Some v -> Some v.Name
                | None -> None
            )
        $"""{eqs |> List.map (Equation.toString true) |> String.concat "\n"}"""


    let private varsToStr vars =
        $"""{vars |> List.map (Variable.toString true) |> String.concat ", "}"""


    let printException = function
    | Exceptions.ValueRangeEmptyValueSet ->
        "ValueRange cannot have an empty value set"

    | Exceptions.EquationEmptyVariableList ->
        "An equation should at least contain one variable"

    | Exceptions.SolverInvalidEquations eqs ->
        $"The following equations are invalid {eqs |> eqsToStr} "

    | Exceptions.ValueRangeMinLargerThanMax (min, max) ->
        $"{min} is larger than {max}"

    | Exceptions.ValueRangeNotAValidOperator ->
        "The value range operator was invalid or unknown"

    | Exceptions.EquationDuplicateVariables vars ->
        $"""The list of variables for the equation contains duplicates
{vars |> List.map (Variable.getName >> Name.toString) |> String.concat ", "}
"""

    | Exceptions.NameLongerThan1000 s ->
        $"This name contains more than 1000 chars: {s}"

    | Exceptions.NameNullOrWhiteSpaceException ->
        "A name cannot be a blank string"

    | Exceptions.VariableCannotSetValueRange (var, vlr) ->
        $"This variable:\n{var |> Variable.toString true}\ncannot be set with this range:{vlr |> ValueRange.toString true}\n"

    | Exceptions.SolverTooManyLoops eqs ->
        $"""The following equations are looped more than {Constants.MAX_LOOP_COUNT} times the equation list count
{eqs |> eqsToStr}
"""
    | Exceptions.ValueRangeEmptyIncrement -> "Increment can not be an empty set"

    | Exceptions.ValueRangeTooManyValues c ->
        $"Trying to calculate with {c} values, which is higher than the max calc count {Constants.MAX_CALC_COUNT}"


    let printMsg = function
    | ExceptionMessage m ->
        m
        |> printException
    | SolverMessage m ->
        let toString eq =
            let op = if eq |> Equation.isProduct then " * " else " + "
            let varName = Variable.getName >> Variable.Name.toString

            match eq |> Equation.toVars with
            | [] -> ""
            | _::[] -> ""
            | y::xs ->
                $"""{y |> varName } = {xs |> List.map varName |> String.concat op}"""


        match m with
        | EquationCouldNotBeSolved eq ->
            $"=== Cannot solve Equation ===\n{eq |> Equation.toString true}"

        | EquationCalculation (op1, op2, y, x, xs) ->
            $"calculating: {Equation.calculationToString op1 op2 y x xs}"

        | EquationStartedSolving eq ->
            $"=== Start solving Equation ===\n{eq |> toString}"

        | EquationFinishedCalculation (xs, changed) ->
            $"""=== Equation finished calculation ===
{if (not changed) then "No changes" else xs |> varsToStr}
"""

        | EquationFinishedSolving (eq, b) ->
            $"""=== Equation Finished Solving ===
{eq |> Equation.toString true}
{b |> Equation.SolveResult.toString}
"""

        | SolverStartSolving eqs ->
            $"=== Solver Start Solving ===\n{eqs |> eqsToStr}"

        | SolverFinishedSolving eqs ->
            $"=== Solver Finished Solving ===\n{eqs |> eqsToStr}"

        | SolverLoopedQue eqs ->
            $"=== Solver looped que\nwith {eqs |> List.length} equations"

        | ConstraintSortOrder cs ->
            $"""=== Constraint sort order ===
            { cs |> List.map (fun (i, c) ->
                c
                |> Constraint.toString
                |> sprintf "%i: %s" i
            )
            |> String.concat "\n"
            }
            """

        | ConstraintVariableNotFound (c, eqs) ->
            $"""=== Constraint Variable not found ===
            {c
            |> sprintf "Constraint %A cannot be set"
            |> (fun s ->
                eqs
                |> List.map (Equation.toString true)
                |> String.concat "\n"
                |> sprintf "%s\In equations:\%s" s
            )
            }
            """
        | ConstraintLimitSetToVariable (l, var) -> ""

        | ConstraintVariableApplied (c, var) ->
            $"""=== Constraint apply Variable ===
            {c
            |> Constraint.toString
            |> fun s ->
                var
                |> Variable.getName
                |> Name.toString
                |> sprintf "%s apply to %s" s
            }
            """

        | ConstrainedEquationsSolved (c, eqs) ->
            $"""=== Equations solved ===
            {c
            |> Constraint.toString
            |> fun s ->
                eqs
                |> List.sort
                |> List.map (Equation.toString true)
                |> String.concat "\n"
                |> sprintf "Constraint: %s applied to\n%s" s

            }
            """
        | ApiSetVariable (var, eqs) -> ""

        | ApiEquationsSolved eqs -> ""

        | ApiAppliedConstraints (cs, eqs) -> ""


    let logger f =
        {
            Log =
                fun { TimeStamp = _; Level = _; Message = msg } ->
                    match msg with
                    | :? Logging.SolverMessage as m ->
                        m |> printMsg |> f
                    | _ -> $"cannot print msg: {msg}" |> f

        }
