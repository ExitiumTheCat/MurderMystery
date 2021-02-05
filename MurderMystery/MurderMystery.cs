using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;
using Terraria.Localization;
using Terraria.ID;

namespace MurderMystery
{
    [ApiVersion(2, 1)]
    public class MurderMystery : TerrariaPlugin
    {

        public override string Name => "MurderMystery";
        public override Version Version => new Version(1, 0);
        public override string Author => "ExitiumTheCat";
        public override string Description => "";

        public bool GameOngoing;
        public bool GameAllowedToStart;
        public bool ModeratorsParticipating;
        public int BreakTime = 20;
        public int CurrentBreakTime;
        public int SurvivalTimer = 300;
        public int CurrentSurvivalTimer;
        public int GunTimer = 10;
        public int CurrentGunTimer;
        public int Murderer = 0;
        public int Detective = 0;

        public MurderMystery(Main game) : base(game) { }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("tshock.tp.others", SetBreakTime, "setbreaktime"));
            Commands.ChatCommands.Add(new Command("tshock.tp.others", SetSurvivalTimer, "setsurvivaltimer"));
            Commands.ChatCommands.Add(new Command("tshock.tp.others", SetGunTimer, "setguntimer"));
            Commands.ChatCommands.Add(new Command("tshock.tp.others", StartOrEndMM, "murdermystery"));
            Commands.ChatCommands.Add(new Command("tshock.tp.others", ModeratorsParticipate, "moderatorsparticipate"));
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.ServerChat.Deregister(this, OnUpdate);
            }
            base.Dispose(disposing);
        }

        private void SetBreakTime(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                foreach (string input in args.Parameters)
                {
                    int.TryParse(input, out BreakTime);
                    if (BreakTime == 0) BreakTime = 20;
                }
            }
            else
            {
                args.Player.SendErrorMessage("Please input a number in seconds.");
            }
        }
        private void SetSurvivalTimer(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                foreach (string input in args.Parameters)
                {
                    int.TryParse(input, out SurvivalTimer);
                    if (SurvivalTimer == 0) SurvivalTimer = 300;
                }
            }
            else
            {
                args.Player.SendErrorMessage("Please input a number in seconds.");
            }
        }
        private void SetGunTimer(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                foreach (string input in args.Parameters)
                {
                    int.TryParse(input, out GunTimer);
                    if (GunTimer == 0) GunTimer = 10;
                }
            }
            else
            {
                args.Player.SendErrorMessage("Please input a number in seconds.");
            }
        }

        private void StartOrEndMM(CommandArgs args)
        {
            if (GameOngoing)
            {
                GameOngoing = false;
                Main.dayTime = true;
                Main.time = 16200;
                TSPlayer.All.SendMessage("The sky lights up...", Color.LightBlue);
            }
            else
            {
                GameOngoing = true;
                Main.dayTime = false;
                Main.time = 16200;
                NetMessage.SendData((int)PacketTypes.WorldInfo, -1, -1, NetworkText.Empty, Main.worldID, 16200, 1);
                TSPlayer.All.SendMessage("The sky darkens...", Color.Red);
                Murderer = Main.rand.Next(0, TShock.Utils.GetActivePlayerCount());
                Detective = Main.rand.Next(0, TShock.Utils.GetActivePlayerCount());
                CurrentSurvivalTimer = SurvivalTimer * 60;
            }
        }
        private void ModeratorsParticipate(CommandArgs args)
        {
            if (ModeratorsParticipating)
            {
                ModeratorsParticipating = false;
                args.Player.SendSuccessMessage("Moderators are no longer participating.");
            }
            else
            {
                ModeratorsParticipating = true;
                args.Player.SendSuccessMessage("Moderators are now participating.");
            }
        }
        private void OnUpdate(EventArgs args)
        {
            if (GameOngoing && CurrentBreakTime <= 0)
            {
                for (int i = 0; i < TShock.Utils.GetActivePlayerCount(); i++)
                {
                    Player plr = Main.player[i];
                    if (Main.player[i].active)
                    {
                        if (!ModeratorsParticipating && TShock.Players[Detective].HasPermission("tshock.tp.others") || !ModeratorsParticipating && TShock.Players[Murderer].HasPermission("tshock.tp.others"))
                        {
                            Murderer = Main.rand.Next(0, TShock.Utils.GetActivePlayerCount());
                            Detective = Main.rand.Next(0, TShock.Utils.GetActivePlayerCount());
                        }
                        else if (Murderer == Detective)
                        {
                            Murderer = Main.rand.Next(0, TShock.Utils.GetActivePlayerCount());
                        }
                        else if (!GameAllowedToStart)
                        {
                            GameAllowedToStart = true;
                            Commands.HandleCommand(TSPlayer.Server, "/give Handgun " + Main.player[Detective].name);
                            Commands.HandleCommand(TSPlayer.Server, "/give 97 " + Main.player[Detective].name + " 1");
                        }
                        if (GameAllowedToStart)
                        {
                            if (i == Murderer)
                            {
                                plr.inventory[1].netDefaults(3351);
                                plr.inventory[57].netDefaults(4346);
                                plr.inventory[58].netDefaults(0);
                                NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.Empty, TShock.Players[i].Index, 1);
                                NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.Empty, TShock.Players[i].Index, 57);
                                NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.Empty, TShock.Players[i].Index, 58);
                            }
                            if (i == Detective)
                            {
                                if (plr.inventory[1].Name != "Musket Ball")
                                {
                                    if (CurrentGunTimer == 0)
                                    {
                                        CurrentGunTimer = GunTimer * 60;
                                    }
                                    else
                                    {
                                        CurrentGunTimer--;
                                        if (CurrentGunTimer == 1)
                                        {
                                            Commands.HandleCommand(TSPlayer.Server, "/give 97 " + Main.player[Detective].name + " 1");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                for (int i2 = 0; i2 < 58; i2++)
                                {
                                    if (plr.inventory[i2].Name == "Handgun")
                                    {
                                        Detective = i;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (CurrentGunTimer != 0)
                            {
                                Main.player[Detective].statLife = 0;
                                NetMessage.SendData((int)PacketTypes.PlayerHp, -1, -1, NetworkText.Empty, TShock.Players[Detective].Index);
                                TSPlayer.All.SendMessage("The gun has been dropped!", Color.Yellow);
                            }
                        }
                    }
                }
            }
            else if (GameOngoing)
            {
                if (CurrentBreakTime > 0)
                {
                    CurrentBreakTime--;
                }
                if (CurrentBreakTime == 300)
                {
                    TSPlayer.All.SendMessage("Game starting in 5 seconds!", Color.Yellow);
                }
                if (CurrentBreakTime == 1)
                {
                    Murderer = Main.rand.Next(0, TShock.Utils.GetActivePlayerCount());
                    Detective = Main.rand.Next(0, TShock.Utils.GetActivePlayerCount());
                    CurrentSurvivalTimer = SurvivalTimer * 60;
                    CurrentGunTimer = GunTimer * 60;
                }
            }
        }
    }
}