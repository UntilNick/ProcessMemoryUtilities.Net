﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ProcessMemoryUtilities.Memory;

namespace ProcessMemoryUtilities.Test
{
    [TestClass]
    public class ProcessMemoryTests
    {
        public delegate void RemoteFunction();

        private readonly int _processId;
        private readonly RemoteFunction _remoteFunctionDelegate;
        private readonly IntPtr _remoteFunctionPointer;
        private volatile int _counter;

        public ProcessMemoryTests()
        {
            _processId = Process.GetCurrentProcess().Id;

            _remoteFunctionDelegate = new RemoteFunction(TestRemoteThread);
            _remoteFunctionPointer = Marshal.GetFunctionPointerForDelegate(_remoteFunctionDelegate);
        }

        private void TestCreateRemoteThread_1(IntPtr hProcess)
        {
            var originalCounter = _counter;

            IntPtr hThread = ProcessMemory.CreateRemoteThreadEx(hProcess, _remoteFunctionPointer);

            Assert.IsFalse(hThread == IntPtr.Zero);

            Assert.IsTrue(ProcessMemory.WaitForSingleObject(hThread, ProcessMemory.WAIT_TIMEOUT_INFINITE) == WaitObjectResult.Success);

            Assert.IsTrue(ProcessMemory.CloseHandle(hThread));

            Assert.IsTrue(originalCounter + 1 == _counter);
        }

        private void TestCreateRemoteThread_2(IntPtr hProcess)
        {
            var originalCounter = _counter;

            IntPtr hThread = ProcessMemory.CreateRemoteThreadEx(hProcess, _remoteFunctionPointer, IntPtr.Zero);

            Assert.IsFalse(hThread == IntPtr.Zero);

            Assert.IsTrue(ProcessMemory.WaitForSingleObject(hThread, ProcessMemory.WAIT_TIMEOUT_INFINITE) == WaitObjectResult.Success);

            Assert.IsTrue(ProcessMemory.CloseHandle(hThread));

            Assert.IsTrue(originalCounter + 1 == _counter);
        }

        private void TestCreateRemoteThread_3(IntPtr hProcess)
        {
            var originalCounter = _counter;

            IntPtr hThread = ProcessMemory.CreateRemoteThreadEx(hProcess, IntPtr.Zero, IntPtr.Zero, _remoteFunctionPointer, IntPtr.Zero, ThreadCreationFlags.Immediately, IntPtr.Zero);

            Assert.IsFalse(hThread == IntPtr.Zero);

            Assert.IsTrue(ProcessMemory.WaitForSingleObject(hThread, ProcessMemory.WAIT_TIMEOUT_INFINITE) == WaitObjectResult.Success);

            Assert.IsTrue(ProcessMemory.CloseHandle(hThread));

            Assert.IsTrue(originalCounter + 1 == _counter);
        }

        private void TestCreateRemoteThread_4(IntPtr hProcess)
        {
            var originalCounter = _counter;

            uint threadId = 0;
            IntPtr hThread = ProcessMemory.CreateRemoteThreadEx(hProcess, IntPtr.Zero, IntPtr.Zero, _remoteFunctionPointer, IntPtr.Zero, ThreadCreationFlags.Immediately, IntPtr.Zero, ref threadId);

            Assert.IsFalse(hThread == IntPtr.Zero);

            Assert.IsFalse(threadId == 0);

            Assert.IsTrue(ProcessMemory.WaitForSingleObject(hThread, ProcessMemory.WAIT_TIMEOUT_INFINITE) == WaitObjectResult.Success);

            Assert.IsTrue(ProcessMemory.CloseHandle(hThread));

            Assert.IsTrue(originalCounter + 1 == _counter);
        }

        private void TestRemoteThread()
        {
            _counter++;
        }

        [TestMethod]
        public void Close()
        {
            IntPtr handle = ProcessMemory.OpenProcess(ProcessAccessFlags.All, _processId);

            Assert.IsFalse(handle == IntPtr.Zero);

            Assert.IsTrue(ProcessMemory.CloseHandle(handle));
        }

        [TestMethod]
        public void CreateRemoteThreadEx()
        {
            IntPtr handle = ProcessMemory.OpenProcess(ProcessAccessFlags.All, _processId);

            Assert.IsFalse(handle == IntPtr.Zero);

            TestCreateRemoteThread_1(handle);
            TestCreateRemoteThread_2(handle);
            TestCreateRemoteThread_3(handle);
            TestCreateRemoteThread_4(handle);

            Assert.IsTrue(ProcessMemory.CloseHandle(handle));
        }

        [TestMethod]
        public void OpenProcess()
        {
            IntPtr handle = ProcessMemory.OpenProcess(ProcessAccessFlags.All, _processId);

            Assert.IsFalse(handle == IntPtr.Zero);

            ProcessMemory.CloseHandle(handle);
        }

        [TestMethod]
        public void ReadProcessMemory_Pointer()
        {
            IntPtr baseAddress = Marshal.AllocHGlobal(4);
            IntPtr buffer = Marshal.AllocHGlobal(4);
            Marshal.WriteInt32(baseAddress, 1337);
            Marshal.WriteInt32(buffer, 0);

            IntPtr handle = ProcessMemory.OpenProcess(ProcessAccessFlags.All, _processId);

            Assert.IsFalse(handle == IntPtr.Zero);

            Assert.IsTrue(ProcessMemory.ReadProcessMemory(handle, baseAddress, buffer, (IntPtr)4));
            Assert.IsTrue(Marshal.ReadInt32(baseAddress) == Marshal.ReadInt32(buffer));

            Marshal.WriteInt32(buffer, 0);

            IntPtr bytesRead = IntPtr.Zero;

            Assert.IsTrue(ProcessMemory.ReadProcessMemory(handle, baseAddress, buffer, (IntPtr)4, ref bytesRead));
            Assert.IsTrue(Marshal.ReadInt32(baseAddress) == Marshal.ReadInt32(buffer));
            Assert.IsTrue(bytesRead == (IntPtr)4);

            Assert.IsTrue(ProcessMemory.CloseHandle(handle));

            Marshal.FreeHGlobal(baseAddress);
            Marshal.FreeHGlobal(buffer);
        }

        [TestMethod]
        public void ReadProcessMemory_T()
        {
            IntPtr baseAddress = Marshal.AllocHGlobal(4);
            Marshal.WriteInt32(baseAddress, 1337);

            IntPtr handle = ProcessMemory.OpenProcess(ProcessAccessFlags.All, _processId);

            Assert.IsFalse(handle == IntPtr.Zero);

            int buffer = 0;

            Assert.IsTrue(ProcessMemory.ReadProcessMemory(handle, baseAddress, ref buffer));
            Assert.IsTrue(buffer == Marshal.ReadInt32(baseAddress));

            buffer = 0;

            IntPtr bytesRead = IntPtr.Zero;

            Assert.IsTrue(ProcessMemory.ReadProcessMemory(handle, baseAddress, ref buffer, ref bytesRead));
            Assert.IsTrue(buffer == Marshal.ReadInt32(baseAddress));
            Assert.IsTrue(bytesRead == (IntPtr)4);

            Assert.IsTrue(ProcessMemory.CloseHandle(handle));

            Marshal.FreeHGlobal(baseAddress);
        }

        [TestMethod]
        public void ReadProcessMemory_TArray()
        {
            IntPtr baseAddress = Marshal.AllocHGlobal(16);

            for (int i = 0; i < 16; i += 4)
            {
                Marshal.WriteInt32(baseAddress, i, 1337);
            }

            IntPtr handle = ProcessMemory.OpenProcess(ProcessAccessFlags.All, _processId);

            Assert.IsFalse(handle == IntPtr.Zero);

            int[] buffer = new int[4];

            Assert.IsTrue(ProcessMemory.ReadProcessMemory(handle, baseAddress, buffer));

            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.IsTrue(buffer[i] == Marshal.ReadInt32(baseAddress, i * 4));

                buffer[i] = 0;
            }

            IntPtr bytesRead = IntPtr.Zero;

            Assert.IsTrue(ProcessMemory.ReadProcessMemory(handle, baseAddress, buffer, ref bytesRead));
            Assert.IsTrue(bytesRead == (IntPtr)(buffer.Length * 4));

            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.IsTrue(buffer[i] == Marshal.ReadInt32(baseAddress, i * 4));
            }

            Assert.IsTrue(ProcessMemory.CloseHandle(handle));

            Marshal.FreeHGlobal(baseAddress);
        }

        [TestMethod]
        public void VirtualAllocAndFree()
        {
            IntPtr handle = ProcessMemory.OpenProcess(ProcessAccessFlags.All, _processId);

            Assert.IsFalse(handle == IntPtr.Zero);

            IntPtr address = ProcessMemory.VirtualAllocEx(handle, IntPtr.Zero, (IntPtr)1024, AllocationType.Reserve | AllocationType.Commit, MemoryProtectionFlags.ExecuteReadWrite);

            Assert.IsFalse(address == IntPtr.Zero);

            Assert.IsTrue(Marshal.ReadInt32(address) == 0);

            Marshal.WriteInt32(address, 1337);

            Assert.IsTrue(Marshal.ReadInt32(address) == 1337);

            Assert.IsTrue(ProcessMemory.VirtualFreeEx(handle, address, IntPtr.Zero, FreeType.Release));

            ProcessMemory.CloseHandle(handle);
        }

        [TestMethod]
        public void VirtualProtectEx()
        {
            IntPtr handle = ProcessMemory.OpenProcess(ProcessAccessFlags.All, _processId);

            Assert.IsFalse(handle == IntPtr.Zero);

            IntPtr address = ProcessMemory.VirtualAllocEx(handle, IntPtr.Zero, (IntPtr)1024, AllocationType.Reserve | AllocationType.Commit, MemoryProtectionFlags.ExecuteReadWrite);

            MemoryProtectionFlags oldProtection = default;
            Assert.IsTrue(ProcessMemory.VirtualProtectEx(handle, address, (IntPtr)1024, MemoryProtectionFlags.NoAccess, ref oldProtection));

            Assert.IsTrue(oldProtection == MemoryProtectionFlags.ExecuteReadWrite);

            try
            {
                Marshal.WriteInt32(address, 1337);
                throw new Exception();
            }
            catch
            {
            }

            Assert.IsTrue(ProcessMemory.VirtualFreeEx(handle, address, IntPtr.Zero, FreeType.Release));

            ProcessMemory.CloseHandle(handle);
        }

        [TestMethod]
        public void WriteProcessMemory_Pointer()
        {
            IntPtr baseAddress = Marshal.AllocHGlobal(4);
            IntPtr buffer = Marshal.AllocHGlobal(4);
            Marshal.WriteInt32(baseAddress, 0);
            Marshal.WriteInt32(buffer, 1337);

            IntPtr handle = ProcessMemory.OpenProcess(ProcessAccessFlags.All, _processId);

            Assert.IsFalse(handle == IntPtr.Zero);

            Assert.IsTrue(ProcessMemory.WriteProcessMemory(handle, baseAddress, buffer, (IntPtr)4));
            Assert.IsTrue(Marshal.ReadInt32(baseAddress) == Marshal.ReadInt32(buffer));

            Marshal.WriteInt32(baseAddress, 0);

            IntPtr bytesWritten = IntPtr.Zero;

            Assert.IsTrue(ProcessMemory.WriteProcessMemory(handle, baseAddress, buffer, (IntPtr)4, ref bytesWritten));
            Assert.IsTrue(Marshal.ReadInt32(baseAddress) == Marshal.ReadInt32(buffer));
            Assert.IsTrue(bytesWritten == (IntPtr)4);

            Assert.IsTrue(ProcessMemory.CloseHandle(handle));

            Marshal.FreeHGlobal(baseAddress);
            Marshal.FreeHGlobal(buffer);
        }

        [TestMethod]
        public void WriteProcessMemory_T()
        {
            IntPtr baseAddress = Marshal.AllocHGlobal(4);
            int buffer = 1337;
            Marshal.WriteInt32(baseAddress, 0);

            IntPtr handle = ProcessMemory.OpenProcess(ProcessAccessFlags.All, _processId);

            Assert.IsFalse(handle == IntPtr.Zero);

            Assert.IsTrue(ProcessMemory.WriteProcessMemory(handle, baseAddress, buffer));
            Assert.IsTrue(Marshal.ReadInt32(baseAddress) == buffer);

            Marshal.WriteInt32(baseAddress, 0);

            IntPtr bytesWritten = IntPtr.Zero;

            Assert.IsTrue(ProcessMemory.WriteProcessMemory(handle, baseAddress, buffer, ref bytesWritten));
            Assert.IsTrue(Marshal.ReadInt32(baseAddress) == buffer);
            Assert.IsTrue(bytesWritten == (IntPtr)4);

            Assert.IsTrue(ProcessMemory.CloseHandle(handle));

            Marshal.FreeHGlobal(baseAddress);
        }

        [TestMethod]
        public void WriteProcessMemory_TArray()
        {
            IntPtr baseAddress = Marshal.AllocHGlobal(16);
            int[] buffer = new int[4];

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 1337;
                Marshal.WriteInt32(baseAddress, i * 4, 0);
            }

            IntPtr handle = ProcessMemory.OpenProcess(ProcessAccessFlags.All, _processId);

            Assert.IsFalse(handle == IntPtr.Zero);

            Assert.IsTrue(ProcessMemory.WriteProcessMemory(handle, baseAddress, buffer));

            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.IsTrue(Marshal.ReadInt32(baseAddress, i * 4) == 1337);
                Marshal.WriteInt32(baseAddress, i * 4, 0);
            }

            IntPtr bytesWritten = IntPtr.Zero;

            Assert.IsTrue(ProcessMemory.WriteProcessMemory(handle, baseAddress, buffer, ref bytesWritten));
            Assert.IsTrue(bytesWritten == (IntPtr)16);

            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.IsTrue(Marshal.ReadInt32(baseAddress, i * 4) == 1337);
            }

            Assert.IsTrue(ProcessMemory.CloseHandle(handle));

            Marshal.FreeHGlobal(baseAddress);
        }
    }
}
