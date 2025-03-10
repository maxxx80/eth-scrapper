﻿namespace ScrapperDispatcherActor

[<AutoOpen>]
module internal Failure =

  open Dapr.Actors
  open ScrapperModels.ScrapperDispatcher
  open Microsoft.Extensions.Logging
  open Common.Utils

  let MAX_RETRIES_COUNT = 3u

  let failure (env: Env) (data: FailureData) =
    let logger = env.Logger

    task {

      use scope = logger.BeginScope("failure {@data}", data)

      logger.LogDebug("Failure")

      let! state = env.StateStore.Get()

      match state with
      | Some state ->
        let failuresCount =
          match state.Status with
          | Status.Failure failure -> failure.FailuresCount
          | _ -> 0u

        let epoch = env.Date() |> toEpoch

        let state =
          { state with
              Status =
                { Data = data
                  FailuresCount = failuresCount + 1u }
                |> Status.Failure
              Date = epoch }

        if failuresCount < MAX_RETRIES_COUNT then
          logger.LogWarning("Retriable failure with {@state}", state)
          return! runScrapper env state.Request state
        else
          do! env.StateStore.Set state
          logger.LogError("Final failure with {@state}", state)
          return state |> Ok

      | None ->
        logger.LogWarning("Failure {@failure} but state is not found", state)

        return StateNotFound |> Error
    }
