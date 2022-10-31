// HookMonitor Class
// Revision 2 (2004-12-22)
// Copyright (C) 2004 Dennis Dietrich
//
// Released unter the BSD License
// http://www.opensource.org/licenses/bsd-license.php

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Up2dateConsole.Helpers.InactivityMonitor
{
    public class HookMonitor : MonitorBase
    {
		#region Private Fields

		private bool disposed    = false;
		private bool globalHooks = false;

		private int keyboardHookHandle = 0;
		private int mouseHookHandle    = 0;

		private Win32HookProcHandler keyboardHandler = null;
		private Win32HookProcHandler mouseHandler    = null;

		#endregion Private Fields

		#region Public Properties

		/// <summary>
		/// Specifies if the instances monitors mouse events
		/// </summary>
		public override bool MonitorMouseEvents
		{
			get
			{
				return base.MonitorMouseEvents;
			}
			set
			{
				if (disposed)
					throw new ObjectDisposedException("Object has already been disposed");

				if (base.MonitorMouseEvents != value)
				{
					base.MonitorMouseEvents = value;
					if (value)
						RegisterMouseHook(globalHooks);
					else
						UnRegisterMouseHook();
				}
			}
		}

		/// <summary>
		/// Specifies if the instances monitors keyboard events
		/// </summary>
		public override bool MonitorKeyboardEvents
		{
			get
			{
				return base.MonitorKeyboardEvents;
			}
			set
			{
				if (disposed)
					throw new ObjectDisposedException("Object has already been disposed");
				
				if (base.MonitorKeyboardEvents != value)
				{
					base.MonitorKeyboardEvents = value;
					if (value)
						RegisterKeyboardHook(globalHooks);
					else
						UnRegisterKeyboardHook();
				}
			}
		}

		#endregion Public Properties

		#region Constructors

		/// <summary>
		/// Creates a new instance of <see cref="HookMonitor"/>
		/// </summary>
		/// <param name="global">
		/// True if the system-wide activity will be monitored, otherwise only
		/// events in the current thread will be monitored
		/// </param>
		public HookMonitor(bool global) : base()
		{
			globalHooks = global;
			if (MonitorKeyboardEvents)
				RegisterKeyboardHook(globalHooks);
			if (MonitorMouseEvents)
				RegisterMouseHook(globalHooks);
		}

		#endregion Constructors

		#region Deconstructor

		/// <summary>
		/// Deconstructor method for use by the garbage collector
		/// </summary>
		~HookMonitor()
		{
			Dispose(false);
		}

		#endregion Deconstructor

		#region Protected Methods

		/// <summary>
		/// Actual deconstructor in accordance with the dispose pattern
		/// </summary>
		/// <param name="disposing">
		/// True if managed and unmanaged resources will be freed
		/// (otherwise only unmanaged resources are handled)
		/// </param>
		protected override void Dispose(bool disposing)
		{
			if (!disposed)
			{
				disposed = true;
				UnRegisterKeyboardHook();
				UnRegisterMouseHook();
			}
			base.Dispose(disposing);
		}

		#endregion

		#region Private Methods

		private void ResetBase()
		{
			if (TimeElapsed && !ReactivatedRaised)
				OnReactivated(new EventArgs());
			base.Reset();
		}

		private int KeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0)
				ResetBase();
			return User32.CallNextHookEx(keyboardHookHandle, nCode, wParam, lParam);
		}

		private int MouseHook(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0)
				ResetBase();
			return User32.CallNextHookEx(mouseHookHandle, nCode, wParam, lParam);
		}

		private void RegisterKeyboardHook(bool global)
		{
			if (keyboardHookHandle == 0)
			{
				keyboardHandler = new Win32HookProcHandler(KeyboardHook);
				if (global)
					keyboardHookHandle = User32.SetWindowsHookEx(
						(int)Win32Hook.WH_KEYBOARD_LL, keyboardHandler,
						Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]),
						(int)0);
				else
					keyboardHookHandle = User32.SetWindowsHookEx(
						(int)Win32Hook.WH_KEYBOARD, keyboardHandler,
                        (IntPtr)0, User32.GetCurrentThreadId());
                if (keyboardHookHandle == 0)
					base.MonitorKeyboardEvents = false;
			}
		}

		private void UnRegisterKeyboardHook()
		{
			if (keyboardHookHandle != 0)
			{
				if (!User32.UnhookWindowsHookEx(keyboardHookHandle))
					base.MonitorKeyboardEvents = true;
				else
				{
					keyboardHookHandle = 0;
					keyboardHandler = null;
				}
			}
		}

		private void RegisterMouseHook(bool global)
		{
			if (mouseHookHandle == 0)
			{
				mouseHandler = new Win32HookProcHandler(MouseHook);
				if (global)
					mouseHookHandle = User32.SetWindowsHookEx(
						(int)Win32Hook.WH_MOUSE_LL, mouseHandler,
						Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]),
						(int)0);
				else
					mouseHookHandle = User32.SetWindowsHookEx(
						(int)Win32Hook.WH_MOUSE, mouseHandler,
                        (IntPtr)0, User32.GetCurrentThreadId());
                if (mouseHookHandle == 0)
					base.MonitorMouseEvents = false;
			}
		}

		private void UnRegisterMouseHook()
		{
			if (mouseHookHandle != 0)
			{
				if (!User32.UnhookWindowsHookEx(mouseHookHandle))
					base.MonitorMouseEvents = true;
				else
				{
					mouseHookHandle = 0;
					mouseHandler    = null;
				}
			}
		}

		#endregion Private Methods

    }
}
