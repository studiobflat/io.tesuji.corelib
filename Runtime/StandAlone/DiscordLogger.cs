using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

public class DiscordLogger : MonoBehaviour
{
	private static string DISCORD_WEBHOOK = "https://discord.com/api/webhooks/831101633575518228/Km7A0B4f-UXctahO9KPPTDgbKP_aEIEgM_CUazwn8pmEdz4NWcc_lkOu56nrn7a9W3Gc";
	private static bool SHOW_SCREENSHOT;
	
    private static readonly HashSet<string> lastErrors = new HashSet<string>();
	private static readonly string[] filteredMessages =
	{
		"Failed to read input report",
		"FMOD failed to initialize the output",
		"Failed to create device file",
		"IOException: Disk full.",
		"application not found",
	};
	
	static DiscordLogger _api;
	public string webhook = DISCORD_WEBHOOK;
	public bool showScreenshot;
	
	void Awake()
	{
		if (_api != null && _api != this) 
		{
			Destroy(this);
			return;
		}

		_api = this;
		DISCORD_WEBHOOK = webhook;
		SHOW_SCREENSHOT = showScreenshot;
	}
#if UNITY_EDITOR
	
	
	[ContextMenu("Send Test Assert")]
	public void TestAssert()
	{
		UnityEngine.Assertions.Assert.AreEqual(1.0, Random.Range(2,3));
	}

	[ContextMenu("Send Test Exception")]
	public void TestException()
	{
		throw new System.Exception("This is a sample Crash sent from DiscordLogger!");
	}
	
	[ContextMenu("Send Sample DevLog")]
	public void TestDev()
	{
		DevLog("This is a sample dev LOG!");
	}
#endif

	void OnEnable()
	{
		Application.logMessageReceived -= DiscordLogger.HandleLog;
		Application.logMessageReceived += DiscordLogger.HandleLog;
	}

	void OnDisable()
	{
		Application.logMessageReceived -= DiscordLogger.HandleLog;
	}
	
    private static readonly LogType[] allowedLogTypes =
	{
        LogType.Assert,
        LogType.Exception
    };
	
    public static void HandleLog(string msg, string stack, LogType type)
    {
        if (Application.isEditor) return;
		if (!allowedLogTypes.Contains(type)) return;
		
		// Don't spam duplicate messages, ignore some useless ones
		if (lastErrors.Contains(stack) || filteredMessages.Any(msg.Contains)) return;
		
		lastErrors.Add(stack);
		_api.StartCoroutine(_PrepareRequest(msg, stack, type));
    }
    
    public static void DevLog(string msg)
    {
	    if (Application.isEditor) return;

	    var stack = UnityEngine.StackTraceUtility.ExtractStackTrace();
	    if (lastErrors.Contains(stack)) return;
	    
	    lastErrors.Add(stack);
	    _api.StartCoroutine(_PrepareRequest(msg, stack, LogType.Log, false));
    }
	
    private static IEnumerator _PrepareRequest(string msg, string stack, LogType type, bool withScreenshot = true)
    {
        yield return new WaitForEndOfFrame();
        
        var www = UnityWebRequest.Post(DISCORD_WEBHOOK,
	        GetFormData(msg, stack, type, withScreenshot ? ScreenCapture.CaptureScreenshotAsTexture().EncodeToJPG() : null)
	    );
        www.SendWebRequest();
    }

	static string _header;
	public static string GetHeader()
	{
		if (!string.IsNullOrEmpty(_header)) return _header;
		
		var buildType = Debug.isDebugBuild ? "debug" : "release";
		_header = $"App:\t\t{Application.identifier}\nVersion:\tv{Application.version}\nPlatform:\t{Application.platform} ({buildType})";
		return _header;
	}

	static string _logName;
	public static string GetLogCrashID()
	{
		if (!string.IsNullOrEmpty(_logName)) return _logName;

		var arr = Application.identifier.Split('.');
		if (arr.Length == 0) arr = new string[]{"(unknown)"};

		var id = arr[arr.Length-1];
		_logName = $"{id}_v{Application.version}";
		return _logName;
	}

	
	static string _deviceInfo;
	public static string GetDeviceInfo()
	{
		if (!string.IsNullOrEmpty(_deviceInfo)) return _deviceInfo;

		// For readability
		var sb = new StringBuilder();
		sb.AppendLine($"deviceType:		{SystemInfo.deviceType}");
		sb.AppendLine($"deviceModel:		{SystemInfo.deviceModel}");
		sb.AppendLine($"deviceName:		{SystemInfo.deviceName}");
		sb.AppendLine($"operatingSystem:	{SystemInfo.operatingSystem}");
		sb.AppendLine($"operatingSystemFamily:	{SystemInfo.operatingSystemFamily}");
		sb.AppendLine($"deviceUniqueIdentifier:	{SystemInfo.deviceUniqueIdentifier}\n");

		sb.AppendLine($"processorCount:		{SystemInfo.processorCount}");
		sb.AppendLine($"processorFrequency:	{SystemInfo.processorFrequency}");
		sb.AppendLine($"processorType:		{SystemInfo.processorType}");
		sb.AppendLine($"systemMemorySize:	{SystemInfo.systemMemorySize}");
		sb.AppendLine($"graphicsMemorySize:	{SystemInfo.graphicsMemorySize}\n");
		
		sb.AppendLine($"batteryLevel: {SystemInfo.batteryLevel}");
		sb.AppendLine($"batteryStatus: {SystemInfo.batteryStatus}");
		sb.AppendLine($"supportsAudio: {SystemInfo.supportsAudio}");
		sb.AppendLine($"supportsGyroscope: {SystemInfo.supportsGyroscope}");
		sb.AppendLine($"supportsVibration: {SystemInfo.supportsVibration}");
		sb.AppendLine($"supportsAccelerometer: {SystemInfo.supportsAccelerometer}");
		sb.AppendLine($"supportsLocationService: {SystemInfo.supportsLocationService}\n");

		sb.AppendLine($"graphicsDeviceID: {SystemInfo.graphicsDeviceID}");
		sb.AppendLine($"graphicsDeviceName: {SystemInfo.graphicsDeviceName}");
		sb.AppendLine($"graphicsDeviceType: {SystemInfo.graphicsDeviceType}");
		sb.AppendLine($"graphicsDeviceVendor: {SystemInfo.graphicsDeviceVendor}");
		sb.AppendLine($"graphicsDeviceVendorID: {SystemInfo.graphicsDeviceVendorID}");
		sb.AppendLine($"graphicsDeviceVersion: {SystemInfo.graphicsDeviceVersion}");
		sb.AppendLine($"graphicsMultiThreaded: {SystemInfo.graphicsMultiThreaded}");
		sb.AppendLine($"renderingThreadingMode: {SystemInfo.renderingThreadingMode}\n");

		sb.AppendLine($"npotSupport: {SystemInfo.npotSupport}");
		sb.AppendLine($"maxCubemapSize: {SystemInfo.maxCubemapSize}");
		sb.AppendLine($"maxTextureSize: {SystemInfo.maxTextureSize}");
		sb.AppendLine($"supportsShadows: {SystemInfo.supportsShadows}");
		sb.AppendLine($"supportsInstancing: {SystemInfo.supportsInstancing}");
		sb.AppendLine($"supportsRayTracing: {SystemInfo.supportsRayTracing}");
		sb.AppendLine($"supportsMipStreaming: {SystemInfo.supportsMipStreaming}");
		sb.AppendLine($"supportedRenderTargetCount: {SystemInfo.supportedRenderTargetCount}\n");
		
		// Unimportant information
		// sb.Append($"copyTextureSupport: {SystemInfo.copyTextureSupport}\ngraphicsShaderLevel: {SystemInfo.graphicsShaderLevel}\ngraphicsUVStartsAtTop: {SystemInfo.graphicsUVStartsAtTop}\nhasDynamicUniformArrayIndexingInFragmentShaders: {SystemInfo.hasDynamicUniformArrayIndexingInFragmentShaders}\nhasHiddenSurfaceRemovalOnGPU: {SystemInfo.hasHiddenSurfaceRemovalOnGPU}\nhasMipMaxLevel: {SystemInfo.hasMipMaxLevel}\nmaxComputeBufferInputsCompute: {SystemInfo.maxComputeBufferInputsCompute}\nmaxComputeBufferInputsDomain: {SystemInfo.maxComputeBufferInputsDomain}\nmaxComputeBufferInputsFragment: {SystemInfo.maxComputeBufferInputsFragment}\nmaxComputeBufferInputsGeometry: {SystemInfo.maxComputeBufferInputsGeometry}\nmaxComputeBufferInputsHull: {SystemInfo.maxComputeBufferInputsHull}\nmaxComputeBufferInputsVertex: {SystemInfo.maxComputeBufferInputsVertex}\nmaxComputeWorkGroupSize: {SystemInfo.maxComputeWorkGroupSize}\nmaxComputeWorkGroupSizeX: {SystemInfo.maxComputeWorkGroupSizeX}\nmaxComputeWorkGroupSizeY: {SystemInfo.maxComputeWorkGroupSizeY}\nmaxComputeWorkGroupSizeZ: {SystemInfo.maxComputeWorkGroupSizeZ}\nsupportedRandomWriteTargetCount: {SystemInfo.supportedRandomWriteTargetCount}\nsupports2DArrayTextures: {SystemInfo.supports2DArrayTextures}\nsupports32bitsIndexBuffer: {SystemInfo.supports32bitsIndexBuffer}\nsupports3DRenderTextures: {SystemInfo.supports3DRenderTextures}\nsupports3DTextures: {SystemInfo.supports3DTextures}\nsupportsAsyncCompute: {SystemInfo.supportsAsyncCompute}\nsupportsAsyncGPUReadback: {SystemInfo.supportsAsyncGPUReadback}\nsupportsCubemapArrayTextures: {SystemInfo.supportsCubemapArrayTextures}\nsupportsGeometryShaders: {SystemInfo.supportsGeometryShaders}\nsupportsGraphicsFence: {SystemInfo.supportsGraphicsFence}\nsupportsHardwareQuadTopology: {SystemInfo.supportsHardwareQuadTopology}\nsupportsMultisampleAutoResolve: {SystemInfo.supportsMultisampleAutoResolve}\nsupportsMultisampledTextures: {SystemInfo.supportsMultisampledTextures}\nsupportsRawShadowDepthSampling: {SystemInfo.supportsRawShadowDepthSampling}\nsupportsMotionVectors: {SystemInfo.supportsMotionVectors}\nsupportsSeparatedRenderTargetsBlend: {SystemInfo.supportsSeparatedRenderTargetsBlend}\nsupportsSetConstantBuffer: {SystemInfo.supportsSetConstantBuffer}\nsupportsSparseTextures: {SystemInfo.supportsSparseTextures}\nsupportsTessellationShaders: {SystemInfo.supportsTessellationShaders}\nsupportsTextureWrapMirrorOnce: {SystemInfo.supportsTextureWrapMirrorOnce}\nunsupportedIdentifier: {SystemInfo.unsupportedIdentifier}\nusesLoadStoreActions: {SystemInfo.usesLoadStoreActions}\nusesReversedZBuffer: {SystemInfo.usesReversedZBuffer}");

		_deviceInfo = sb.ToString();
		return _deviceInfo;
	}


	const double MEGABYTE = 1024 * 1024;
	
	public static string GetDeviceStatus()
	{
		var sb = new StringBuilder();
		sb.AppendLine($"Total Memory:    {System.GC.GetTotalMemory(false)/MEGABYTE, 10:F2} MB");
		sb.AppendLine($"Mono Heap:       {Profiler.GetMonoHeapSizeLong()/MEGABYTE, 10:F2} MB");
		sb.AppendLine($"Mono Used:       {Profiler.GetMonoUsedSizeLong()/MEGABYTE, 10:F2} MB");
		sb.AppendLine($"Allocated:       {Profiler.GetTotalAllocatedMemoryLong()/MEGABYTE, 10:F2} MB");
		sb.AppendLine($"Reserved:        {Profiler.GetTotalReservedMemoryLong()/MEGABYTE, 10:F2} MB");
		sb.AppendLine($"Unused Reserved: {Profiler.GetTotalUnusedReservedMemoryLong()/MEGABYTE, 10:F2} MB");
		sb.AppendLine($"Temp Allocator:  {Profiler.GetTempAllocatorSize()/MEGABYTE, 10:F2} MB");

		return sb.ToString(); // Do not cache!
	}

	// static int logCounter = 1;
    private static WWWForm GetFormData(string msg, string stack, LogType type, byte[] screenshot)
    {
        WWWForm formData = new WWWForm();

		string pMessage = msg;

		switch (type)
		{
			case LogType.Log:
			{
				pMessage = $"[Dev]: \n{msg}"; 
				break;
			}
			
			case LogType.Error:
			{
				pMessage = $"[LogError]: \n{msg}"; 
				break;
			} 
			
			case LogType.Exception:
			{
				pMessage = msg.Contains("AssertionException: ") ? $"[Assert]: \n{msg}" : $"[Exception]: \n{msg}";
				//pMessage = $"{msg.Replace("AssertionException: ",  "[Assert]: ").Replace("Exception: ",  "[Exception]: ")}";
				break;
			}
		}

        formData.AddField("content", $"```css\n{pMessage}\n```");

		var logContent = System.Text.Encoding.UTF8.GetBytes
		(
			$"{GetHeader()}\n\n{msg}\n\n{stack}\n-----\n\n{GetDeviceStatus()}\n-----\n\n{GetDeviceInfo()}"
		);

		var logId = GetLogCrashID();
		formData.AddBinaryData(logId, logContent, $"{logId}.txt");

		if (screenshot == null) return formData;
		
		var dat = SHOW_SCREENSHOT ? string.Empty : ".dat";
        formData.AddBinaryData("screenshot", screenshot, $"screenshot.jpg{dat}");
        return formData;
    }
}