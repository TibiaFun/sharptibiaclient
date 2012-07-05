﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CTC
{
    public delegate void StateChangedEventHandler(ClientState NewState);

    class GameDesktop : UIView
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
        }

        #region Data Members and Properties

        List<ClientState> Clients = new List<ClientState>();

        TopTaskbar Taskbar;
        GameSidebar Sidebar;
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

        // Methods
        public void AddClient(ClientState State)
        {
            Clients.Add(State);
            if (Clients.Count == 1)
                ActiveClient = State;

            // Read in some state (in case the game was fast-forwarded)
            foreach (ClientContainer Container in State.Viewport.Containers.Values)
                OnOpenContainer(State.Viewport, Container);

            // Hook up handlers for some events
            State.Viewport.OpenContainer += OnOpenContainer;
            State.Viewport.CloseContainer += OnCloseContainer;
            Frame.AddClient(State);
        }

        #region Event Handlers

        void OnResize(object o, EventArgs args)
        {
            if (Context.Window.ClientBounds.Height > 0 && Context.Window.ClientBounds.Width > 0)
            {
                // Change the size of this view
                Bounds.Width = Context.Window.ClientBounds.Width;
                Bounds.Height = Context.Window.ClientBounds.Height;

                NeedsLayout = true;
            }
        }

        /// <summary>
        /// We override this to handle captured devices
        /// </summary>
        /// <param name="mouse"></param>
        /// <returns></returns>
        public override bool MouseLeftClick(MouseState mouse)
        {
            if (Context.MouseFocusedPanel != null)
                return Context.MouseFocusedPanel.MouseLeftClick(mouse);

            // We use a copy so that event handling can modify the list
            List<UIView> SubviewListCopy = new List<UIView>(Children);
            foreach (UIView subview in SubviewListCopy)
            {
                if (subview.AcceptsMouseEvent(mouse))
                    if (subview.MouseLeftClick(mouse))
                        return true;
            }

            return false;
        }

        public override bool MouseMove(MouseState mouse)
        {
            // To get mouse move events you must capture the mouse first
            if (Context.MouseFocusedPanel != null)
                return Context.MouseFocusedPanel.MouseMove(mouse);
            return false;
        }

        protected void OnOpenContainer(ClientViewport Viewport, ClientContainer Container)
        {
            ContainerPanel Panel = new ContainerPanel(Viewport, Container.ContainerID);
            Panel.Bounds.X = 1000;
            Panel.Bounds.Y = Viewport.Containers.Count * 100;
            Panel.Bounds.Height = 100;
            Panel.ZOrder = 1;
            AddSubview(Panel);
        }

        protected void OnCloseContainer(ClientViewport Viewport, ClientContainer Container)
        {
            foreach (ContainerPanel CPanel in SubviewsOfType<ContainerPanel>())
                if (CPanel.ContainerID == Container.ContainerID)
                    CPanel.RemoveFromSuperview();
        }

        #endregion

        public override void LayoutSubviews()
        {
            // Resize the sidebar to fit
            Sidebar.Bounds = new Rectangle
            {
                X = Context.Window.ClientBounds.Width - Sidebar.Bounds.Width,
                Y = 0,
                Height = Context.Window.ClientBounds.Height,
                Width = Sidebar.Bounds.Width
            };

            base.LayoutSubviews();
        }

        public override void Update(GameTime Time)
        {
            Boolean newSkin = Context.SkinChanged;
            Context.Update(Time);

            LFPS.Enqueue(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            // Remove ticks older than one second
            while (LFPS.Count > 0 && LFPS.First() < DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - 1000)
                LFPS.Dequeue();

            foreach (ClientState State in Clients)
                State.Update(Time);

            base.Update(Time);

            // Layout needs to be done in two steps after skin change
            // First step to remeasure all interface elements, second to
            // position them.
            if (newSkin)
                NeedsLayout = true;
        }

        #region Drawing Code

        public override void Draw(SpriteBatch NullBatch, Rectangle BoundingBox)
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
            DrawBackgroundChildren(ForegroundBatch, Bounds);
            DrawForegroundChildren(ForegroundBatch, Bounds);
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

        public void CreatePanels()
        {
            Taskbar = new TopTaskbar();
            AddSubview(Taskbar);

            Frame = new GameFrame();
            Frame.Bounds.X = 10;
            Frame.Bounds.Y = 20;
            Frame.Bounds.Width = 800;
            Frame.Bounds.Height = 600;
            Frame.ZOrder = -1;
            AddSubview(Frame);

            Sidebar = new GameSidebar();

            SkillPanel Skills = new SkillPanel();
            Skills.Bounds.X = 4;
            Skills.Bounds.Y = Sidebar.ClientBounds.Top;
            Sidebar.AddWindow(Skills);

            VIPPanel VIPs = new VIPPanel();
            VIPs.Bounds.X = 4;
            VIPs.Bounds.Y = 210;
            Sidebar.AddWindow(VIPs);

            InventoryPanel Inventory = new InventoryPanel();
            Inventory.Bounds.X = 4;
            Inventory.Bounds.Y = 410;
            Sidebar.AddWindow(Inventory);

            AddSubview(Sidebar);

            Chat = new ChatPanel();
            Chat.Bounds.X = 10;
            Chat.Bounds.Y = 640;
            Chat.Bounds.Height = 150;
            Chat.Bounds.Width = 800;
            Chat.ZOrder = 0;
            AddSubview(Chat);

            // Register listeners
            ActiveStateChanged += Skills.OnNewState;
            ActiveStateChanged += VIPs.OnNewState;
            ActiveStateChanged += Inventory.OnNewState;
            ActiveStateChanged += Chat.OnNewState;
            ActiveStateChanged += Chat.OnNewState;
        }

        #endregion
    }
}
