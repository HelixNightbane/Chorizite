﻿using AcClient;
using Autofac;
using Iced.Intel;
using Chorizite.ACProtocol;
using Chorizite.ACProtocol.Enums;
using Chorizite.Core;
using Chorizite.Core.Backend;
using Chorizite.Core.Dats;
using Chorizite.Core.Extensions;
using Chorizite.Core.Input;
using Chorizite.Core.Net;
using Chorizite.Core.Render;
using Chorizite.Loader.Standalone.Input;
using Chorizite.Loader.Standalone.Render;
using Microsoft.Extensions.Logging;
using System;
using Chorizite.Common;
using Chorizite.Loader.Standalone.Hooks;
using ChatType = Chorizite.Core.Backend.ChatType;
using System.Text;
using System.Linq;
using SharpDX;

namespace Chorizite.Loader.Standalone {
    public unsafe class ACChoriziteBackend : IClientBackend, IChoriziteBackend {
        private int _previousGameScreen = (int)UIMode.None;

        public IRenderInterface Renderer { get; }
        public DX9RenderInterface DX9Renderer { get; }

        public IInputManager Input { get; }
        public Win32InputManager Win32Input { get; }

        public int GameScreen {
            get => ((IntPtr)UIFlow.m_instance == IntPtr.Zero || *UIFlow.m_instance is null) ? 0 : (int)(*UIFlow.m_instance)->_curMode;
            set {
                if (!((IntPtr)UIFlow.m_instance == IntPtr.Zero || *UIFlow.m_instance is null)) {
                    if (value != _previousGameScreen) {
                        if ((int)(*UIFlow.m_instance)->_curMode != value) {
                            (*UIFlow.m_instance)->QueueUIMode((UIMode)value);
                        }
                        _previousGameScreen = value;
                        _OnScreenChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        WeakEvent<LogMessageEventArgs> IChoriziteBackend._OnLogMessage { get; } = new();

        private readonly WeakEvent<PacketDataEventArgs> _OnC2SData = new WeakEvent<PacketDataEventArgs>();
        public event EventHandler<PacketDataEventArgs>? OnC2SData {
            add { _OnC2SData.Subscribe(value); }
            remove { _OnC2SData.Unsubscribe(value); }
        }

        private readonly WeakEvent<PacketDataEventArgs> _OnS2CData = new WeakEvent<PacketDataEventArgs>();
        public event EventHandler<PacketDataEventArgs>? OnS2CData {
            add { _OnS2CData.Subscribe(value); }
            remove { _OnS2CData.Unsubscribe(value); }
        }

        private readonly WeakEvent<ChatInputEventArgs> _OnChatInput = new();
        public event EventHandler<ChatInputEventArgs>? OnChatInput {
            add { _OnChatInput.Subscribe(value); }
            remove { _OnChatInput.Unsubscribe(value); }
        }

        private readonly WeakEvent<ChatTextAddedEventArgs> _OnChatTextAdded = new();
        public event EventHandler<ChatTextAddedEventArgs>? OnChatTextAdded {
            add { _OnChatTextAdded.Subscribe(value); }
            remove { _OnChatTextAdded.Unsubscribe(value); }
        }

        private readonly WeakEvent<EventArgs> _OnScreenChanged = new WeakEvent<EventArgs>();
        public event EventHandler<EventArgs>? OnScreenChanged {
            add { _OnScreenChanged.Subscribe(value); }
            remove { _OnScreenChanged.Unsubscribe(value); }
        }

        public static IChoriziteBackend Create(IContainer container) {
            var renderer = new DX9RenderInterface(StandaloneLoader.UnmanagedD3DPtr, container.Resolve<ILogger<DX9RenderInterface>>(), container.Resolve<IDatReaderInterface>());
            var input = new Win32InputManager(container.Resolve<ILogger<Win32InputManager>>());

            return new ACChoriziteBackend(renderer, input);
        }

        private ACChoriziteBackend(DX9RenderInterface renderer, Win32InputManager input) {
            DX9Renderer = renderer;
            Renderer = renderer;
            Win32Input = input;
            Input = input;
        }

        internal void HandleC2SPacketData(byte[] bytes) {
            _OnC2SData?.Invoke(this, new PacketDataEventArgs(MessageDirection.ClientToServer, bytes));
        }

        internal void HandleS2CPacketData(byte[] bytes) {
            _OnS2CData?.Invoke(this, new PacketDataEventArgs(MessageDirection.ServerToClient, bytes));
        }

        internal void HandleChatTextAdded(ChatTextAddedEventArgs eventArgs) {
            _OnChatTextAdded?.Invoke(this, eventArgs);
        }

        internal void HandleChatTextInput(ChatInputEventArgs eventArgs) {
            _OnChatInput?.Invoke(this, eventArgs);
        }

        public bool EnterGame(uint characterId) {
            // Todo: check that it is a valid character id
            if ((*UIFlow.m_instance)->_curMode != UIMode.CharacterManagementUI) {
                return false;
            }
            return AcClient.CPlayerSystem.GetPlayerSystem()->LogOnCharacter(characterId) == 1;
        }

        public void AddChatText(string text, ChatType type = ChatType.Default) {
            ChatHooks.AddChatText(text, (eChatTypes)type);
        }

        private static delegate* unmanaged[Thiscall]<Client*, int> Cleanup = (delegate* unmanaged[Thiscall]<Client*, int>)0x00401EC0;
        private static delegate* unmanaged[Thiscall]<Client*, void> CleanupNet = (delegate* unmanaged[Thiscall]<Client*, void>)0x00412060;

        public void Exit() {
            CleanupNet(*Client.m_instance);
            Cleanup(*Client.m_instance);
        }

        public void Dispose() {
            Renderer?.Dispose();
            Input?.Dispose();
        }
    }
}
