using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Net.Http;
using System.Threading.Tasks;

/// <summary>
/// Davinci - A powerful, esay-to-use image downloading and caching library for Unity in Run-Time
/// v 1.2
/// Developed by ShamsDEV.com
/// copyright (c) ShamsDEV.com All Rights Reserved.
/// Licensed under the MIT License.
/// https://github.com/shamsdev/davinci
/// </summary>
public class Davinci : MonoBehaviour
{
    public static bool staticLog = true;
    static GameObject componentHolder;
    static readonly Dictionary<int, Davinci> underProcessDavincies = new Dictionary<int, Davinci>();
    static readonly HttpClient httpClient = new HttpClient();
    static readonly string directory = Application.persistentDataPath + "/davinci/";

    enum RendererType
    {
        none,
        uiImage,
        renderer,
        sprite,
    }

    RendererType rendererType = RendererType.none;
    GameObject targetObject;
    Texture2D loadingPlaceholder;
    Texture2D errorPlaceholder;
    string url;
    string filePath;
    float fadeTime = 1;
    bool logEnabled = false;
    bool cached = true;
    int uniqueHash;
    int progress;

    UnityAction OnStartAction;
    UnityAction OnDownloadedAction;
    UnityAction OnLoadedAction;
    UnityAction OnEndAction;
    UnityAction<int> OnDownloadProgressChange;
    UnityAction<string> OnErrorAction;

    event Action<Texture2D> ImageReady;

    /// <summary>
    /// Get instance of davinci class
    /// </summary>
    public static Davinci get()
    {
        if (componentHolder == null)
            componentHolder = new GameObject("Davinci");

        return componentHolder.AddComponent<Davinci>();
    }

    /// <summary>
    /// Set image url for download.
    /// </summary>
    /// <param name="url">Image Url</param>
    /// <returns></returns>
    public Davinci load(string url)
    {
        if (logEnabled)
            Debug.Log("[Davinci] Url set : " + url);

        this.url = url;
        return this;
    }

    /// <summary>
    /// Set fading animation time.
    /// </summary>
    /// <param name="fadeTime">Fade animation time. Set 0 for disable fading.</param>
    /// <returns></returns>
    public Davinci setFadeTime(float fadeTime)
    {
        if (logEnabled)
            Debug.Log("[Davinci] Fading time set : " + fadeTime);

        this.fadeTime = fadeTime;
        return this;
    }

    /// <summary>
    /// Set target Image component.
    /// </summary>
    /// <param name="image">target Unity UI image component</param>
    /// <returns></returns>
    public Davinci into(Image image)
    {
        if (logEnabled)
            Debug.Log("[Davinci] Target as UIImage set : " + image);

        rendererType = RendererType.uiImage;
        targetObject = image.gameObject;
        return this;
    }

    /// <summary>
    /// Set target Renderer component.
    /// </summary>
    /// <param name="renderer">target renderer component</param>
    /// <returns></returns>
    public Davinci into(Renderer renderer)
    {
        if (logEnabled)
            Debug.Log("[Davinci] Target as Renderer set : " + renderer);

        rendererType = RendererType.renderer;
        targetObject = renderer.gameObject;
        return this;
    }

    public Davinci into(SpriteRenderer spriteRenderer)
    {
        if (logEnabled)
            Debug.Log("[Davinci] Target as SpriteRenderer set : " + spriteRenderer);

        rendererType = RendererType.sprite;
        targetObject = spriteRenderer.gameObject;
        return this;
    }

    public Davinci withStartAction(UnityAction action)
    {
        OnStartAction = action;

        if (logEnabled)
            Debug.Log("[Davinci] On start action set : " + action);

        return this;
    }

    public Davinci withDownloadedAction(UnityAction action)
    {
        OnDownloadedAction = action;

        if (logEnabled)
            Debug.Log("[Davinci] On downloaded action set : " + action);

        return this;
    }

    public Davinci withDownloadProgressChangedAction(UnityAction<int> action)
    {
        OnDownloadProgressChange = action;

        if (logEnabled)
            Debug.Log("[Davinci] On download progress changed action set : " + action);

        return this;
    }

    public Davinci withLoadedAction(UnityAction action)
    {
        OnLoadedAction = action;

        if (logEnabled)
            Debug.Log("[Davinci] On loaded action set : " + action);

        return this;
    }

    public Davinci withErrorAction(UnityAction<string> action)
    {
        OnErrorAction = action;

        if (logEnabled)
            Debug.Log("[Davinci] On error action set : " + action);

        return this;
    }

    public Davinci withEndAction(UnityAction action)
    {
        OnEndAction = action;

        if (logEnabled)
            Debug.Log("[Davinci] On end action set : " + action);

        return this;
    }

    /// <summary>
    /// Show or hide logs in console.
    /// </summary>
    /// <param name="enable">'true' for show logs in console.</param>
    /// <returns></returns>
    public Davinci setEnableLog(bool enableLog)
    {
        logEnabled = enableLog;

        if (enableLog)
            Debug.Log("[Davinci] Logging enabled : " + enableLog);

        return this;
    }

    /// <summary>
    /// Set the sprite of image when davinci is downloading and loading image
    /// </summary>
    /// <param name="loadingPlaceholder">loading texture</param>
    /// <returns></returns>
    public Davinci setLoadingPlaceholder(Texture2D loadingPlaceholder)
    {
        this.loadingPlaceholder = loadingPlaceholder;

        if (logEnabled)
            Debug.Log("[Davinci] Loading placeholder has been set.");

        return this;
    }

    /// <summary>
    /// Set image sprite when some error occurred during downloading or loading image
    /// </summary>
    /// <param name="errorPlaceholder">error texture</param>
    /// <returns></returns>
    public Davinci setErrorPlaceholder(Texture2D errorPlaceholder)
    {
        this.errorPlaceholder = errorPlaceholder;

        if (logEnabled)
            Debug.Log("[Davinci] Error placeholder has been set.");

        return this;
    }

    /// <summary>
    /// Enable cache
    /// </summary>
    /// <returns></returns>
    public Davinci setCached(bool cached)
    {
        this.cached = cached;

        if (logEnabled)
            Debug.Log("[Davinci] Cache enabled : " + cached);

        return this;
    }

    /// <summary>
    /// Start davinci process.
    /// </summary>
    public void start()
    {
        if (url == null)
        {
            error("Url has not been set. Use 'load' funtion to set image url.");
            return;
        }

        try
        {
            Uri uri = new Uri(url);
            url = uri.AbsoluteUri;
        }
        catch (Exception exception)
        {
            error("URL error: " + exception.Message);
            return;
        }

        if (rendererType == RendererType.none || targetObject == null)
        {
            error("Target has not been set. Use 'into' function to set target component.");
            return;
        }

        if (logEnabled)
            Debug.Log("[Davinci] Start Working.");

        if (loadingPlaceholder != null)
            SetLoadingImage();

        OnStartAction?.Invoke();

        uniqueHash = url.GetHashCode();
        filePath = directory + uniqueHash;

        if (underProcessDavincies.ContainsKey(uniqueHash) == false)
        {
            underProcessDavincies.Add(uniqueHash, this);
            if (File.Exists(filePath))
                LoadFile();
            else
                Download();
        }
        else
            underProcessDavincies[uniqueHash].ImageReady += ShowImage;
    }


    void error(string message)
    {
        if (logEnabled)
            Debug.LogError("[Davinci] Error : " + message);

        OnErrorAction?.Invoke(message);

        if (errorPlaceholder != null)
            ShowImage(errorPlaceholder);
        else
            finish();
    }

    void SetLoadingImage()
    {
        switch (rendererType)
        {
            case RendererType.renderer:
                Renderer renderer = targetObject.GetComponent<Renderer>();
                renderer.material.mainTexture = loadingPlaceholder;
                break;

            case RendererType.uiImage:
                Image image = targetObject.GetComponent<Image>();
                Sprite sprite = Sprite.Create(loadingPlaceholder,
                    new Rect(0, 0, loadingPlaceholder.width, loadingPlaceholder.height),
                    new Vector2(0.5f, 0.5f));
                image.sprite = sprite;
                break;

            case RendererType.sprite:
                SpriteRenderer spriteRenderer = targetObject.GetComponent<SpriteRenderer>();
                Sprite spriteImage = Sprite.Create(loadingPlaceholder,
                    new Rect(0, 0, loadingPlaceholder.width, loadingPlaceholder.height),
                    new Vector2(0.5f, 0.5f));
                spriteRenderer.sprite = spriteImage;
                break;
        }
    }

    async void LoadFile()
    {
        FileStream fileStream = null;
        byte[] imageData;

        try
        {
            fileStream = new FileStream(filePath, FileMode.Open);
            imageData = new byte[fileStream.Length];
            await fileStream.ReadAsync(imageData, 0, imageData.Length);
        }
        catch (Exception exception)
        {
            error("Load file error: " + exception.Message);
            return;
        }
        finally
        {
            fileStream?.Dispose();
        }

        OnLoadedAction?.Invoke();
        ImageDataReady(imageData);
    }


    async void Download()
    {
        byte[] imageData;

        try
        {
            HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(url);
            imageData = await httpResponseMessage.Content.ReadAsByteArrayAsync();
        }
        catch (Exception exception)
        {
            error("Download error: " + exception.Message);
            return;
        }

        if (cached == true)
            await SaveFile(imageData);

        OnDownloadedAction?.Invoke();
        ImageDataReady(imageData);
    }

    async Task SaveFile(byte[] imageData)
    {
        FileStream fileStream = null;

        try
        {
            if (Directory.Exists(directory) == false)
                Directory.CreateDirectory(directory);

            fileStream = new FileStream(filePath, FileMode.Create);
            await fileStream.WriteAsync(imageData, 0, imageData.Length);
        }
        catch (Exception exception)
        {
            error("Save file error: " + exception.Message);
        }
        finally
        {
            fileStream?.Dispose();
        }
    }

    void ImageDataReady(byte[] imageData)
    {
        progress = 100;
        OnDownloadProgressChange?.Invoke(progress);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageData);
        underProcessDavincies.Remove(uniqueHash);
        ImageReady?.Invoke(texture);
        ShowImage(texture);
    }

    async void ShowImage(Texture2D texture)
    {
        switch (rendererType)
        {
            case RendererType.renderer:
                Renderer renderer = targetObject.GetComponent<Renderer>();

                if (renderer == null || renderer.material == null)
                    break;

                renderer.material.mainTexture = texture;
                float maxAlpha;

                if (fadeTime > 0 && renderer.material.HasProperty("_Color"))
                {
                    maxAlpha = renderer.material.color.a;
                    await Tween(x => { Color color = renderer.material.color; color.a = x; renderer.material.color = color; }, 0f, maxAlpha, fadeTime);
                }


                break;

            case RendererType.uiImage:
                Image image = targetObject.GetComponent<Image>();

                if (image == null)
                    break;

                Sprite sprite = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                image.sprite = sprite;
                maxAlpha = image.color.a;

                if (fadeTime > 0)
                {
                    await Tween(x => { Color color = image.color; color.a = x; image.color = color; }, 0f, maxAlpha, fadeTime);
                }
                break;

            case RendererType.sprite:
                SpriteRenderer spriteRenderer = targetObject.GetComponent<SpriteRenderer>();

                if (spriteRenderer == null)
                    break;

                Sprite spriteImage = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                spriteRenderer.sprite = spriteImage;
                maxAlpha = spriteRenderer.color.a;

                if (fadeTime > 0)
                {
                    await Tween(x => { Color color = spriteRenderer.color; color.a = x; spriteRenderer.color = color; }, 0f, maxAlpha, fadeTime);
                }
                break;
        }


        if (logEnabled)
            Debug.Log("[Davinci] Image has been loaded.");

        finish();
    }

    void finish()
    {
        if (logEnabled)
            Debug.Log("[Davinci] Operation has been finished.");

        OnEndAction?.Invoke();

        Destroy(this, 1f);
    }

    /// <summary>
    /// Clear a certain cached file with its url
    /// </summary>
    /// <param name="url">Cached file url.</param>
    /// <returns></returns>
    public static void ClearCache(string url)
    {
        try
        {
            File.Delete(directory + url.GetHashCode());

            if (staticLog)
                Debug.Log($"[Davinci] Cached file has been cleared: {url}");
        }
        catch (Exception exception)
        {
            if (staticLog)
                Debug.LogError("[Davinci] Error while removing cached file: " + exception.Message);
        }
    }

    /// <summary>
    /// Clear all davinci cached files
    /// </summary>
    /// <returns></returns>
    public static void ClearAllCachedFiles()
    {
        try
        {
            Directory.Delete(directory, true);

            if (staticLog)
                Debug.Log("[Davinci] All Davinci cached files has been cleared.");
        }
        catch (Exception exception)
        {
            if (staticLog)
                Debug.LogError("[Davinci] Error while removing cached file: " + exception.Message);
        }
    }

    float from;
    float to;
    float duration;
    float currentTime;
    TaskCompletionSource<bool> tcs;
    Action<float> action;

    async Task Tween(Action<float> action, float from, float to, float duration)
    {
        tcs = new TaskCompletionSource<bool>();
        this.from = from;
        this.to = to;
        this.duration = duration;
        this.action = action;
        action(from);
        enabled = true;
        await tcs.Task;
    }

    void Start()
    {
        enabled = false;
    }

    void Update()
    {
        currentTime += Time.deltaTime;

        if (currentTime >= duration)
        {
            action(to);
            tcs.SetResult(true);
            enabled = false;
        }
        else if (currentTime > 0)
            action(from + (to - from) * currentTime / duration);
    }


}