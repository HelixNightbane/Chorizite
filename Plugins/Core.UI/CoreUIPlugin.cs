﻿using ACUI.Lib;
using ACUI.Lib.RmlUi;
using Autofac;
using Core.DatService;
using MagicHat.Core.Input;
using MagicHat.Core.Plugins;
using MagicHat.Core.Plugins.AssemblyLoader;
using MagicHat.Core.Render;
using Microsoft.Extensions.Logging;
using RmlUiNet;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ACUI {
    /// <summary>
    /// This is the main plugin class. When your plugin is loaded, Startup() is called, and when it's unloaded Shutdown() is called.
    /// </summary>
    public class CoreUIPlugin : IPluginCore {
        private readonly IPluginManager _pluginManager;
        private readonly IRenderInterface _renderer;
        private readonly IInputManager _input;
        private readonly ILogger<CoreUIPlugin>? _log;

        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string path);

        public static UI UI { get; private set; }

        protected CoreUIPlugin(AssemblyPluginManifest manifest, IPluginManager pluginManager, IRenderInterface renderer, IInputManager input, ILogger<CoreUIPlugin>? log) : base(manifest) {
            _log = log;
            _pluginManager = pluginManager;
            _renderer = renderer;
            _input = input;
            UI = new UI(manifest);
            try {
                // we need to manually load RmlUiNative.dll with an absolute path, or DllImport will
                // fail to find it later
                _log?.LogDebug($"Manually pre-loading {Path.Combine(AssemblyDirectory, "RmlUiNative.dll")}");
                LoadLibrary(Path.Combine(AssemblyDirectory, "RmlUiNative.dll"));

                UI.Init(_pluginManager, _renderer, _input, _log);

                _renderer.OnGraphicsPreReset += PluginManager_OnGraphicsPreReset;
                _renderer.OnGraphicsPostReset += PluginManager_OnGraphicsPostReset;
            }
            catch (Exception ex) {
                _log?.LogError(ex, "Error during initialization ");
            }
        }

        public CoreUIPlugin(AssemblyPluginManifest manifest) : base(manifest) {
        }

        private void PluginManager_OnGraphicsPreReset(object sender, EventArgs e) {
            UI.Dispose();
        }

        private void PluginManager_OnGraphicsPostReset(object sender, EventArgs e) {
            UI.Init(_pluginManager, _renderer, _input, _log);
        }
        
        /// <summary>
        /// Called when your plugin is unloaded. Either when logging out, closing the client, or hot reloading.
        /// </summary>
        protected override void Dispose() {
            try {
                _log?.LogTrace($"Shutting down");
                UI.Dispose();
            }
            catch (Exception ex) {
                _log?.LogError(ex, "Error during shutdown");
            }
        }
    }
}
