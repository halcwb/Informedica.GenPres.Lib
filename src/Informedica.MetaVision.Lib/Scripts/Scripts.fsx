

#load "load.fsx"


#load "../MetaVision.fs"


open DocumentFormat.OpenXml.Office2021.Excel.RichDataWebImage
open Informedica.Utils.Lib.BCL
open Informedica.ZIndex.Lib
open Informedica.MetaVision.Lib
open Informedica.ZIndex.Lib.ATCGroup
open Informedica.ZIndex.Lib.GenericProduct



MetaVision.getDrugFamilies "DrugFamilies"


MetaVision.routeShapeUnits "RouteShapeUnits"


MetaVision.createRoutes (Some "data/output/DrugDatabaseForImport.xlsx") "Routes"


MetaVision.createDoseForms (Some "data/output/DrugDatabaseForImport.xlsx") "DoseForms"


let gpks = 
    [|
        //161144 //aardbeiblad/wijnstokblad kauwtablet 40/40mg
        //165573 //abacavir drank 20mg/ml
        2194 //paracetamol tablet  500mg
        30392 // paracetamol drank 24mg/ml
        143618 //hydrocortison/fusidinezuur zalf 10/20mg/g
        121967 //paracetamol infvlst 10mg/ml
        43079 //amoxicilline/clavulaanzuur tablet 500/125mg
        53856 //amoxicilline/clavulaanzuur injpdr 1000/200mg
        82708 //ondansetron injvlst 2mg/ml
        88749 //noradrenaline infopl conc 1mg/ml
        134546 //morfine infvlst  10mg/ml
    |]


let init () =
    Array.empty
    |> MetaVision.createImport MetaVision.config


MetaVision.insertAdditionalIngredients()



let meds =
    GenPresProduct.get true 
    |> Array.filter (fun gpp ->
        gpp.GenericProducts
        |> Array.exists (fun gp -> 
            gpks |> Array.exists ((=) gp.Id) 
        )
    )
    |> MetaVision.createImport MetaVision.config


type OrderingStyle =
    | NoInfusedOver
    | SpecifyInfuseOver
    | SetDoseAndRate


let orderingStyleToString = function
    | NoInfusedOver -> "Geen looptijd", "No Infuse Over Drug - Calculate Total Volume"
    | SpecifyInfuseOver -> "Looptijd specificeren", "Specify Infuse Over Drug - Set Infuse Over"
    | SetDoseAndRate -> "", ""


let mapTempl = (Array.mapStringHeadings Constants.orderTemplateHeadings) >> (String.concat "\t")


let nonSolutionMeds =
    meds
    |> Array.collect (fun m ->
        m.Routes
        |> String.splitAt ';'
        |> Array.collect (fun r ->
            if m.Products |> Array.isEmpty then
                [| m, "", r  |]
            else
                m.Products
                |> Array.map (fun p ->
                    m, p.ProductName, r
                )
        )
        |> Array.filter (fun (m, _, r) ->
            m.IsSolution
            |> not
        )
    )


nonSolutionMeds
|> Array.map (fun (m, _, r) ->
    m.MedicationName,
    r,
    m.DoseForms
)
|> Array.iter (printfn "%A")


nonSolutionMeds
|> Array.map (fun (m, p, r) ->
    let ordStyle = NoInfusedOver 
    {|
        OrderTemplateName = m.MedicationName
        MedicationName = m.MedicationName
        ProductName = p
        DoseForm = m.DoseForms
        Route = r
        IsPRN = "FALSE"
        PatternMode = "Standard"
        Frequency =
            m.Frequencies
            |> String.splitAt ';'
            |> Array.tryHead
            |> Option.defaultValue ""
        ComponentType = "MainComponent"
        OrderingStyle = ordStyle |> orderingStyleToString |> fst
        LockerTemplate = ordStyle |> orderingStyleToString |> snd
        ComponentMedicationName =
            if ordStyle = NoInfusedOver then ""
            else
                if p |> String.isNullOrWhiteSpace then m.MedicationName
                else ""
        ComponentProductName =
            if ordStyle = NoInfusedOver then ""
            else
                if p |> String.isNullOrWhiteSpace then ""
                else p
        ComponentQuantityVolumeValue =
            if m.Unit = "keer" then 1.
            else
                match m.ComplexMedications |> Array.tryHead with
                | Some cm -> cm.Concentration
                | None -> 0.
        ComponentQuantityVolumeUnit =
            if m.Unit = "keer" then "keer"
            else
                match m.ComplexMedications |> Array.tryHead with
                | Some cm -> cm.ConcentrationUnit
                | None -> ""
        ComponentConcentrationMassUnit =
            if m.Unit = "keer" then "keer"
            else
                match m.ComplexMedications |> Array.tryHead with
                | Some cm -> cm.ConcentrationUnit
                | None -> ""
        ComponentConcentrationVolumeUnit = "mL"
        TotalVolumeUnit = "mL"
        StartMethod = "Volgende geplande dosis"
        EndMethod = "Geen tijdslimiet"
        WeightType = "ActualWeight"
        Comment = ""
        Caption = ""
        AvailableInRT = "TRUE"
    |}
)
|> Array.filter (fun r -> r.ComponentQuantityVolumeValue > 0.)
|> Array.map (fun r ->
    [|
        "OrderTemplateName", r.OrderTemplateName
        "MedicationName", r.MedicationName
        "ProductName", r.ProductName
        "DoseForm", r.DoseForm
        "Route", r.Route
        "IsPRN", r.IsPRN
        "PatternMode",r.PatternMode
        "Frequency", r.Frequency
        "ComponentType", r.ComponentType
        "OrderingStyle", r.OrderingStyle
        "OrderTemplateName", r.OrderTemplateName
        "ComponentMedicationName", r.ComponentMedicationName
        "ComponentProductName", r.ComponentProductName
        "ComponentQuantityVolumeValue", $"{r.ComponentQuantityVolumeValue |> Double.toStringNumberNLWithoutTrailingZeros}"
        "ComponentQuantityVolumeUnit", r.ComponentQuantityVolumeUnit
        "ComponentConcentrationMassUnit", r.ComponentConcentrationMassUnit
        "ComponentConcentrationVolumeUnit", r.ComponentConcentrationVolumeUnit
        "TotalVolumeUnit", r.TotalVolumeUnit
        "StartMethod", r.StartMethod
        "EndMethod", r.EndMethod
        "WeightType", r.WeightType
        "Comment", r.Comment
        "Caption", r.Caption
        "AvailableInRT", r.AvailableInRT
    |]
    |> mapTempl
)
|> print MetaVision.config.ImportFile "OrderTemplates"



open Informedica.Utils.Lib.BCL


ATCGroup.get ()
|> Array.filter (fun g -> g.ATC5 |> String.contains "V04CL")



let gps =
    GenPresProduct.get true
    |> Array.collect (fun gpp -> gpp.GenericProducts)
    |> Array.map (fun gp -> gp.Name)
    |> Array.distinct
    |> Array.length


GenPresProduct.get true
|> Array.collect (fun gpp -> gpp.GenericProducts)
|> fun xs ->
    let x = xs |> Array.head

    xs
//    |> Array.tail
    |> Array.fold (fun acc gp ->
        let acc =
            if acc |> Array.exists (fun (x,_) -> x.Label = gp.Label) then acc
            else acc |> Array.append [| (gp, 0) |]

        acc
        |> Array.map (fun (x, n) ->
            if x.Label = gp.Label then (x, n + 1) else (x, n)
        )
    ) [| (x, 0) |]
    |> Array.filter (fun (_, n) -> n > 1)
    |> Array.map (fun (x, _) ->
        x.Label
    )
    |> Array.sort
    |> Array.iter (printfn "%s")
//    |> Array.length






GenPresProduct.get true
|> MetaVision.createSolutions "Solutions.csv"
|> Array.length


let rts1 =
    MetaVision.createRoutes "Routes.csv"
    |> Array.map (String.split "\t")
    |> Array.map List.toArray
    |> Array.map (Array.item 1)
    |> Array.skip 1

let rts2 =
    MetaVision.createDoseForms "DoseForms.csv"
    |> Array.map (String.split "\t")
    |> Array.map List.toArray
    |> Array.map (Array.item 2)
    |> Array.collect (String.splitAt ';')
    |> Array.skip 1
    |> Array.distinct

rts1 |> Array.length
rts2 |> Array.length

rts2 |> Array.forall (fun r -> rts1 |> Array.exists ((=) r))


let forms1 =
    MetaVision.createDoseForms "DoseForms.csv"
    |> Array.map (String.split "\t")
    |> Array.map List.toArray
    |> Array.map (Array.item 1)
    |> Array.skip 1
    |> Array.distinct

let meds =
    GenPresProduct.get true
    |> MetaVision.createMedications "Ingredients.csv" "Medications.csv" "ComplexMedications.csv" "Brands.csv" "Products.csv"

let forms2 =
    meds
    |> Array.map (String.split "\t")
    |> Array.map (List.toArray)
    |> Array.map (Array.item 9)
    |> Array.skip 1
    |> Array.distinct

// all forms in meds should exist in forms
forms2 |> Array.forall (fun f -> forms1 |> Array.exists ((=) f))

let rts3 =
    meds
    |> Array.map (String.split "\t")
    |> Array.map List.toArray
    |> Array.map (Array.item 10)
    |> Array.collect (String.splitAt ';')
    |> Array.skip 1
    |> Array.distinct

// all routes in meds should exists in routes
rts3 |> Array.forall (fun r ->
    let b = rts1 |> Array.exists ((=) r)
    if not b then printfn $"||{r}||"
    b
)

