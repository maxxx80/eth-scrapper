﻿namespace ScrapperDispatcherActor

[<RequireQualifiedAccess>]
module NextBlockRangeCalc2 =
  open ScrapperModels

  let private successNextBlockRangeCalc (maxEthItemsInResponse: uint) (result: Success) (itemsPerBlock: float list) =
    let avgItemsPerBlock = itemsPerBlock |> List.average

    let blocksToRequest = (float maxEthItemsInResponse) / avgItemsPerBlock
    // Minus some sqew
    let blocksToRequest =
      (blocksToRequest - (blocksToRequest * 0.1))
      |> System.Convert.ToUInt32

    let to' = result.BlockRange.To + blocksToRequest

    let resultRange =
      { From = result.BlockRange.To
        To = to' }

    resultRange

  let private errorNextBlockRangeCalc (error: Error) ranges =
    match error.Data with
    | LimitExceeded ->
      // Decrease range by half from `from`
      { From = error.BlockRange.From
        To =
          error.BlockRange.From
          + (error.BlockRange.To - error.BlockRange.From) / 2u }
    | EmptyResult ->
      // Increase by half from `to`
      { From = error.BlockRange.To
        To =
          error.BlockRange.To
          + (error.BlockRange.To - error.BlockRange.From) * 2u }
    | Unknown ->
      // Left as it is
      { From = error.BlockRange.From
        To = error.BlockRange.To }

  let calc maxEthItemsInResponse itemsPerRange =
    function
    | Ok success -> successNextBlockRangeCalc maxEthItemsInResponse success itemsPerRange
    | Error error -> errorNextBlockRangeCalc error itemsPerRange
