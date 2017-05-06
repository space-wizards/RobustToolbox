// Old code from the ClientConsoleHost.cs file before refactoring to modularized commands.
// Here so they're not forgotten about but I'm not gonna implement them RIGHT NOW.

//case "addgas":
//    if (args.Count > 1 && Convert.ToDouble(args[1]) > 0)
//    {
//        if (player != null)
//        {
//            double amount = Convert.ToDouble(args[1]);
//            var t =
//                map.GetFloorAt(
//                    player.GetComponent<ITransformComponent>(ComponentFamily.Transform).Position) as
//                Tile;
//            if (t != null)
//                t.GasCell.AddGas((float) amount, GasType.Toxin);
//            SendConsoleReply(amount.ToString() + " Gas added.", sender);
//        }
//    }
//    break;
//case "heatgas":
//    if (args.Count > 1 && Convert.ToDouble(args[1]) > 0)
//    {
//        if (player != null)
//        {
//            double amount = Convert.ToDouble(args[1]);
//            var t =
//                map.GetFloorAt(
//                    player.GetComponent<ITransformComponent>(ComponentFamily.Transform).Position) as
//                Tile;
//            if (t != null)
//                t.GasCell.AddGas((float) amount, GasType.Toxin);
//            SendConsoleReply(amount.ToString() + " Gas added.", sender);
//        }
//    }
//    break;
//case "atmosreport":
//    IoCManager.Resolve<IAtmosManager>().TotalAtmosReport();
//    break;
//case "tpvreport": // Reports on temp / pressure
//    if (player != null)
//    {
//        var ti =
//            (Tile)
//            map.GetFloorAt(player.GetComponent<ITransformComponent>(ComponentFamily.Transform).Position);
//        if (ti == null)
//            break;
//        GasCell ce = ti.gasCell;
//        SendConsoleReply("T/P/V: " + ce.GasMixture.Temperature.ToString() + " / " + ce.GasMixture.Pressure.ToString() + " / " + ce.GasVelocity.ToString(), sender);
//        //var chatMgr = IoCManager.Resolve<IChatManager>();
//        //chatMgr.SendChatMessage(ChatChannel.Default,
//        //                        "T/P/V: " + ce.GasMixture.Temperature.ToString() + " / " +
//        //                        ce.GasMixture.Pressure.ToString() + " / " + ce.GasVelocity.ToString(),
//        //                        "TempCheck",
//        //                        0);
//    }
//    break;
//case "gasreport":
//    if (player != null)
//    {
//        var tile = map.GetFloorAt(player.GetComponent<ITransformComponent>(ComponentFamily.Transform).Position) as Tile;
//        if (tile == null)
//            break;
//        GasCell c = tile.gasCell;
//        for (int i = 0; i < c.GasMixture.gasses.Length; i++)
//        {
//            SendConsoleReply(((GasType) i).ToString() + ": " +c.GasMixture.gasses[i].ToString(CultureInfo.InvariantCulture) + " m", sender);
//            //var chatMgr = IoCManager.Resolve<IChatManager>();
//            //chatMgr.SendChatMessage(ChatChannel.Default,
//            //                        ((GasType) i).ToString() + ": " +
//            //                        c.GasMixture.gasses[i].ToString(CultureInfo.InvariantCulture) + " m",
//            //                        "GasReport", 0);
//        }
//    }
//    break;
