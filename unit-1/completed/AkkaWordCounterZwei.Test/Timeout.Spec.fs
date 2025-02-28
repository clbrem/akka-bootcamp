namespace AkkaWordCounterZwei.Test
open Akka.Actor
open Akka.FSharp

open Xunit
open Akka.TestKit.Xunit2
open Xunit.Abstractions

type private Testing =
    | Ping
    | Pong
    

type Timeout_Spec(helper: ITestOutputHelper) as this =
    inherit TestKit(config="akka.loglevel=DEBUG",output=helper)
    


