

#load "load.fsx"


#load "../Types.fs"
#load "../Data.fs"
#load "../Utils.fs"
#load "../MetaVision.fs"


open Informedica.ZIndex.Lib
open Informedica.MetaVision.Lib


GenPresProduct.get true
|> MetaVision.createImport { MetaVision.config with IncludeAssortment = [| NEO |] }//[| UMCU; ICC; ICK;  NEO |] }



