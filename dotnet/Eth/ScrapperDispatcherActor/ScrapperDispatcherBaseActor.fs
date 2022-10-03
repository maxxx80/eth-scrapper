﻿namespace ScrapperDispatcherActor

[<AutoOpen>]
module ScrapperDispatcherBaseActor =

  open Dapr.Actors
  open ScrapperModels.ScrapperDispatcher

  type ScrapperDispatcherBaseActor(env: Env) =

    // override set state in order to report manager immediately
    let setState (state: State) =
      let manager = env.CreateJobManagerActor(state.ParentId)

      task {
        do! env.SetState state
        printfn "setState %O" state
        manager.ReportJobState { Job = state; ActorId = env.ActorId }
        |> ignore

        return ()
      }
      :> System.Threading.Tasks.Task

    let env = { env with SetState = setState }

    interface IScrapperDispatcherActor with

      member this.Start data = start env data

      member this.Continue data = continue env data

      member this.Pause() = pause env

      member this.Resume() = resume env

      member this.State() = env.GetState()

      member this.Reset() = env.RemoveState()

      member this.Failure(data: FailureData) = failure env data

      member this.ConfirmContinue(data: ConfirmContinueData) = confirmContinue env data
