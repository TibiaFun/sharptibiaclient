﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public delegate void StateChangedEventHandler(ClientState NewState);

    class GameDesktop : UIPanel
    {
        public GameDesktop(GameWindow Window, GraphicsDeviceManager Graphics, ContentManager Content)
            : base(new UIContext(Window, Graphics, Content))
        {
            // Store window size
            Bounds.X = 0;
            Bounds.Y = 0;
            Bounds.Width = Window.ClientBounds.Width;
            Bounds.Height = Window.ClientBounds.Height;

            // Listener when window changes size
            Window.ClientSizeChanged += new EventHandler<EventArgs>(OnResize);

            // Create all the panels we need
            CreatePanels();
        }

        #region Data Members and Properties

        List<ClientState> Clients = new List<ClientState>();

        TopTaskbar Taskbar;
        SkillPanel Skills;
        VIPPanel VIPs;
        InventoryPanel Inventory;
        ChatPanel Chat;
        GameFrame Frame;

        SpriteBatch ForegroundBatch;

        protected ClientState ActiveClient
        {
            get
            {
                return Clients[0];
            }
            set
            {
                ActiveStateChanged(value);
            }
        }

        Queue<long> LFPS = new Queue<long>();
        Queue<long> GFPS = new Queue<long>(); 

        #endregion

        #region Event Slots

        public event StateChangedEventHandler ActiveStateChanged;

        #endregion

        // Event handlers
        void OnResize(object o, EventArgs args)
        {
            if (Context.Window.ClientBounds.Height > 0 && Context.Window.ClientBounds.Width > 0)
            {
                Bounds.Width = Context.Window.ClientBounds.Width;
                Bounds.Height = Context.Window.ClientBounds.Height;
            }
        }

        // Properties
        public void AddClient(ClientState State)
        {
            Clients.Add(State);
            if (Clients.Count == 1)
                ActiveClient = State;
            Frame.AddClient(State);
            InsertChildPanel(0, Frame);
        }


        public override void Update(GameTime Time)
        {
            Context.GameTime = Time;

            LFPS.Enqueue(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            while (LFPS.Count > 0 && LFPS.First() < DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - 1000)
                LFPS.Dequeue();

            foreach (ClientState State in Clients)
                State.Update(Time);

            Skills.Bounds.X = Bounds.Width - Skills.Bounds.Width - 50;
            Skills.Bounds.Y = 200;

            VIPs.Bounds.X = Bounds.Width - VIPs.Bounds.Width - 50;
            VIPs.Bounds.Y = 400;

            Inventory.Bounds.X = Bounds.Width - Inventory.Bounds.Width - 50;
            Inventory.Bounds.Y = 100;

            Chat.Bounds.X = 10;
            Chat.Bounds.Y = 640;

            base.Update(Time);
        }

        #region Drawing Code

        public override void Draw(SpriteBatch NullBatch)
        {
            ForegroundBatch.Begin();

            string o = "";

            GFPS.Enqueue(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            while (GFPS.Count > 0 && GFPS.First() < DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - 1000)
                GFPS.Dequeue();

            o += " LFPS: " + LFPS.Count;
            o += " GFPS: " + GFPS.Count;
            o += " RCTC";

            // Find the center of the string
            Vector2 FontOrigin = Context.StandardFont.MeasureString(o);
            FontOrigin.X = Context.Window.ClientBounds.Width - FontOrigin.X - 4;
            FontOrigin.Y = 4;

            // Draw the string
            ForegroundBatch.DrawString(
                Context.StandardFont, o, FontOrigin,
                Color.LightGreen, 0.0f, new Vector2(0.0f, 0.0f),
                1.0f, SpriteEffects.None, 0.5f);

            // Draw UI
            DrawChildren(ForegroundBatch);
            // End draw UI

            ForegroundBatch.End();
        }

        #endregion


        #region Loading Code

        public void Load()
        {
            Context.Load();

            ForegroundBatch = new SpriteBatch(Context.Graphics.GraphicsDevice);
        }

        private void CreatePanels()
        {
            Taskbar = new TopTaskbar(this);
            AddChildPanel(Taskbar);

            Frame = new GameFrame(this);
            Frame.Bounds.X = 10;
            Frame.Bounds.Y = 20;
            AddChildPanel(Frame);

            Skills = new SkillPanel(this);
            AddChildPanel(Skills);

            VIPs = new VIPPanel(this);
            AddChildPanel(VIPs);

            Inventory = new InventoryPanel(this);
            AddChildPanel(Inventory);

            Chat = new ChatPanel(this);
            Chat.Bounds.Height = 150;
            Chat.Bounds.Width = 800;
            AddChildPanel(Chat);

            // Register listeners
            ActiveStateChanged += Skills.OnNewState;
            ActiveStateChanged += VIPs.OnNewState;
            ActiveStateChanged += Inventory.OnNewState;
            ActiveStateChanged += Chat.OnNewState;
        }

        #endregion
    }
}