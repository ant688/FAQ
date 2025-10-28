using CommunityToolkit.Mvvm.Messaging;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using Lepo.i18n;
using PDFWand.Helpers;
using PDFWand.Messenger;
using PDFWand.Models;
using PDFWand.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using System.Windows.Threading;

namespace PDFWand.ViewModels.Pages;

public partial class PagingViewModel : ObservableObject, INavigationAware
{
    private readonly IContentDialogService _contentDialogService;
    private LocalizationSet? _localizationSet;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly ILocalizationProvider _localizationProvider;
    private readonly IMessenger _messenger;
    private string _defaultPagingContent;
    private CancellationTokenSource? _previewAllCancellation;

    private static readonly IReadOnlyDictionary<string, string> FontMapping = new Dictionary<string, string>
    {
        ["HELVETICA"] = StandardFonts.HELVETICA,
        ["HELVETICA_OBLIQUE"] = StandardFonts.HELVETICA_OBLIQUE,
        ["HELVETICA_BOLD"] = StandardFonts.HELVETICA_BOLD,
        ["HELVETICA_BOLDOBLIQUE"] = StandardFonts.HELVETICA_BOLDOBLIQUE,
        ["COURIER"] = StandardFonts.COURIER,
        ["COURIER_OBLIQUE"] = StandardFonts.COURIER_OBLIQUE,
        ["COURIER_BOLD"] = StandardFonts.COURIER_BOLD,
        ["COURIER_BOLDOBLIQUE"] = StandardFonts.COURIER_BOLDOBLIQUE,
        ["TIMES_ROMAN"] = StandardFonts.TIMES_ROMAN,
        ["TIMES_ITALIC"] = StandardFonts.TIMES_ITALIC,
        ["TIMES_BOLD"] = StandardFonts.TIMES_BOLD
    };

    private const float DefaultMargin = 36f;

    public PagingViewModel(IContentDialogService contentDialogService,
        ILocalSettingsService localSettingsService,
        ILocalizationProvider localizationProvider,
        IMessenger messenger)
    {
        _contentDialogService = contentDialogService;
        _localSettingsService = localSettingsService;
        _localizationProvider = localizationProvider;
        _messenger = messenger;
        _localizationSet = _localizationProvider.GetLocalizationSet(_localizationProvider.GetCulture().Name);
        _defaultPagingContent = _localizationSet?["PagingDefaultText"] ?? "Page";
        PagingContent = _defaultPagingContent;

        _messenger.Register<LanguageChangedMessage>(this, (_, message) =>
        {
            _localizationSet = _localizationProvider.GetLocalizationSet(message.Culture.Name);
            var newDefaultPagingContent = _localizationSet?["PagingDefaultText"] ?? "Page";
            if (string.Equals(PagingContent, _defaultPagingContent, StringComparison.CurrentCulture))
            {
                PagingContent = newDefaultPagingContent;
            }

            _defaultPagingContent = newDefaultPagingContent;
            OnPropertyChanged(nameof(DefaultPagingContent));
            OnPropertyChanged(nameof(PreviewPagingText));
        });

        OnPropertyChanged(nameof(DefaultPagingContent));
        OnPropertyChanged(nameof(PreviewPagingText));
    }

    public Task OnNavigatedFromAsync()
    {
        CancelPreviewAllRendering();
        ZoomLevel = 1.0;
        UpdateZoomCommandStates();
        _isManualZoom = false;
        return Task.CompletedTask;
    }

    public async Task OnNavigatedToAsync()
    {
        var isPreviewAllPage = await _localSettingsService.ReadSettingAsync<bool>("PreviewAllPage");
        if (isPreviewAllPage)
        {
            PreviewAllPage = true;
            PreviewOnePage = false;
        }
        else
        {
            PreviewAllPage = false;
            PreviewOnePage = true;
        }
    }

    #region Preview Handling

    [ObservableProperty]
    private ImageSource? _currentViewPage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPagingCommand))]
    private string? _selectPath;

    [ObservableProperty]
    private bool _hasSelectedFile;

    [ObservableProperty]
    private Visibility _openedFilePathVisibility = Visibility.Collapsed;

    private PdfiumViewer.PdfDocument? _pdfDocument;

    private const double MinZoomLevel = 0.5;
    private const double MaxZoomLevel = 3.0;
    private const double ZoomStep = 0.1;
    private bool _isManualZoom;
    private double _previewAvailableWidth;
    private double _previewAvailableHeight;
    private double _currentPagePixelWidth;
    private double _currentPagePixelHeight;

    [ObservableProperty]
    private ObservableCollection<PdfPreviewPage> _previewPages = [];


    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private int _totalPage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrevPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private int _currentPageIndex = 0;

    [ObservableProperty]
    private bool _previewAllPage = true;

    [ObservableProperty]
    private bool _previewOnePage = false;

    partial void OnPreviewAllPageChanged(bool value)
    {
        if (!value)
        {
            CancelPreviewAllRendering();
            return;
        }

        if (_pdfDocument is null || TotalPage <= 0)
        {
            return;
        }

        CancelPreviewAllRendering();
        PreviewPages.Clear();
        _previewAllCancellation = new CancellationTokenSource();
        _ = LoadAllPreviewsAsync(_pdfDocument, _previewAllCancellation);
    }

    partial void OnPreviewOnePageChanged(bool value)
    {
        if (!value)
        {
            CurrentViewPage = null;
            return;
        }

        if (_pdfDocument is not null && CurrentPageIndex > -1)
        {
            _ = PreViewPageAsync(_pdfDocument, CurrentPageIndex);
        }
    }

    [ObservableProperty]
    private string? _masterPassword;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ZoomInCommand))]
    [NotifyCanExecuteChangedFor(nameof(ZoomOutCommand))]
    private double _zoomLevel = 1.0;

    partial void OnCurrentPageIndexChanged(int value)
    {
        if (_pdfDocument != null && value > -1)
        {
            _ = PreViewPageAsync(_pdfDocument, value);
        }

        OnPropertyChanged(nameof(PreviewPagingText));
    }

    partial void OnTotalPageChanged(int value)
    {
        OnPropertyChanged(nameof(PreviewPagingText));
    }


    [RelayCommand]
    private async Task OpenFile()
    {
        Microsoft.Win32.OpenFileDialog o = new()
        {
            Filter = "PDF file|*.pdf",
            Title = _localizationSet!["OpenFile"],
            InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString()
        };
        if (o.ShowDialog() == true)
        {
            CancelPreviewAllRendering();
            SelectPath = o.FileName;
            _isManualZoom = false;
            ZoomLevel = 1.0;
OpenPDF:
            try
            {
                _pdfDocument?.Dispose();
                _pdfDocument = null;
                _pdfDocument = PdfiumViewer.PdfDocument.Load(SelectPath, MasterPassword);
                TotalPage = _pdfDocument.PageCount;
                UpdateZoomCommandStates();
            }
            catch (PdfiumViewer.PdfException ex)
            {
                if (ex.Message == "Password required or incorrect password")
                {
                    var dlgContent = new Wpf.Ui.Controls.PasswordBox
                    {
                        PlaceholderText = _localizationSet["Password"]
                    };
                    var dlg = new ContentDialog
                    {
                        Title = _localizationSet!["PasswordToOpen"],
                        Content = dlgContent,
                        PrimaryButtonText = _localizationSet!["Open"],
                        CloseButtonText = _localizationSet!["Cancel"]
                    };
                    var rs = await _contentDialogService.ShowAsync(dlg, CancellationToken.None);
                    if (rs == ContentDialogResult.Primary)
                    {
                        MasterPassword = dlgContent.Password;
                        goto OpenPDF;
                    }
                    else
                    {
                        SelectPath = null;
                        return;
                    }
                }
                else
                {
                    SelectPath = null;
                    return;
                }
            }
            catch (Exception ex)
            {
                var dlg = new ContentDialog
                {
                    Title = "Open error",
                    Content = ex.Message,
                };
                await _contentDialogService.ShowAsync(dlg, CancellationToken.None);
                SelectPath = null;
                return;
            }




            CurrentPageIndex = 0;

            if (PreviewOnePage && _pdfDocument is not null)
            {
                await PreViewPageAsync(_pdfDocument, CurrentPageIndex);
            }

            if (PreviewAllPage && _pdfDocument is not null)
            {
                PreviewPages.Clear();
                CancelPreviewAllRendering();
                _previewAllCancellation = new CancellationTokenSource();
                _ = LoadAllPreviewsAsync(_pdfDocument, _previewAllCancellation);
            }
        }
    }


    private async Task PreViewPageAsync(PdfiumViewer.PdfDocument pdfDoc, int page, float dpiX = 96, float dpiY = 96)
    {
        var sizeInPoints = pdfDoc.PageSizes[page];
        int widthInPixels = (int)Math.Round(sizeInPoints.Width * dpiX / 72F);
        int heightInPixels = (int)Math.Round(sizeInPoints.Height * dpiY / 72F);

        var bitmap = await Task.Run(() =>
        {
            return RenderPageToMemDC(pdfDoc, page, widthInPixels, heightInPixels, dpiX, dpiY);
        });

        if (_pdfDocument == pdfDoc && CurrentPageIndex == page)
        {
            CurrentViewPage = bitmap;
        }
    }

    private async Task LoadAllPreviewsAsync(PdfiumViewer.PdfDocument document, CancellationTokenSource cancellationSource)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        const int batchSize = 4;
        var cancellationToken = cancellationSource.Token;

        try
        {
            await Task.Run(() =>
            {
                var pageSizes = document.PageSizes;
                var totalPages = document.PageCount;
                var batch = new List<PdfPreviewPage>(batchSize);

                for (int i = 0; i < totalPages; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var sizeInPoints = pageSizes[i];
                    int widthInPixels = (int)Math.Round(sizeInPoints.Width * 96f / 72f);
                    int heightInPixels = (int)Math.Round(sizeInPoints.Height * 96f / 72f);

                    var previewPage = new PdfPreviewPage
                    {
                        PageNo = i + 1,
                        TotalPage = totalPages,
                        ViewPage = RenderPageToMemDC(document, i, widthInPixels, heightInPixels)
                    };

                    batch.Add(previewPage);

                    if (batch.Count >= batchSize)
                    {
                        DispatchPreviewBatch(dispatcher, document, batch.ToArray(), cancellationToken);
                        batch.Clear();
                    }
                }

                if (batch.Count > 0)
                {
                    DispatchPreviewBatch(dispatcher, document, batch.ToArray(), cancellationToken);
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(_previewAllCancellation, cancellationSource))
            {
                _previewAllCancellation.Dispose();
                _previewAllCancellation = null;
            }
            else
            {
                cancellationSource.Dispose();
            }
        }
    }

    private void DispatchPreviewBatch(Dispatcher? dispatcher, PdfiumViewer.PdfDocument document, PdfPreviewPage[] batch, CancellationToken cancellationToken)
    {
        if (batch.Length == 0)
        {
            return;
        }

        if (dispatcher is null)
        {
            if (_pdfDocument != document || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            foreach (var page in batch)
            {
                PreviewPages.Add(page);
            }

            return;
        }

        dispatcher.Invoke(() =>
        {
            if (_pdfDocument != document || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            foreach (var page in batch)
            {
                PreviewPages.Add(page);
            }
        });
    }

    private void CancelPreviewAllRendering()
    {
        if (_previewAllCancellation is null)
        {
            return;
        }

        if (!_previewAllCancellation.IsCancellationRequested)
        {
            _previewAllCancellation.Cancel();
        }
        _previewAllCancellation = null;
    }

    [RelayCommand(CanExecute = nameof(CanZoomIn))]
    private void ZoomIn()
    {
        _isManualZoom = true;
        if (ZoomLevel >= MaxZoomLevel)
        {
            return;
        }

        var newZoom = Math.Min(MaxZoomLevel, ZoomLevel + ZoomStep);
        ZoomLevel = Math.Round(newZoom, 2);
    }

    [RelayCommand(CanExecute = nameof(CanZoomOut))]
    private void ZoomOut()
    {
        _isManualZoom = true;
        if (ZoomLevel <= MinZoomLevel)
        {
            return;
        }

        var newZoom = Math.Max(MinZoomLevel, ZoomLevel - ZoomStep);
        ZoomLevel = Math.Round(newZoom, 2);
    }

    private bool CanZoomIn()
    {
        return _pdfDocument != null && ZoomLevel < MaxZoomLevel;
    }

    private bool CanZoomOut()
    {
        return _pdfDocument != null && ZoomLevel > MinZoomLevel;
    }

    private void UpdateZoomCommandStates()
    {
        ZoomInCommand.NotifyCanExecuteChanged();
        ZoomOutCommand.NotifyCanExecuteChanged();
        FitToScreenCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanFitToScreen))]
    private void FitToScreen()
    {
        _isManualZoom = false;
        UpdateZoomFitIfNeeded(true);
    }

    private bool CanFitToScreen()
    {
        return _pdfDocument is not null && CurrentViewPage is not null && PreviewOnePage;
    }

    public void UpdatePreviewViewportSize(double width, double height)
    {
        _previewAvailableWidth = width;
        _previewAvailableHeight = height;
        UpdateZoomFitIfNeeded();
    }

    private void UpdateZoomFitIfNeeded(bool force = false)
    {
        if ((!_isManualZoom || force) && PreviewOnePage)
        {
            if (_currentPagePixelWidth <= 0 || _currentPagePixelHeight <= 0 ||
                _previewAvailableWidth <= 0 || _previewAvailableHeight <= 0)
            {
                if (force)
                {
                    ZoomLevel = 1.0;
                    _isManualZoom = false;
                }

                return;
            }

            double widthScale = _previewAvailableWidth / _currentPagePixelWidth;
            double heightScale = _previewAvailableHeight / _currentPagePixelHeight;
            double fitScale = Math.Min(widthScale, heightScale);

            if (!double.IsFinite(fitScale) || fitScale <= 0)
            {
                fitScale = 1.0;
            }

            var clampedScale = Math.Clamp(fitScale, MinZoomLevel, MaxZoomLevel);
            ZoomLevel = Math.Round(clampedScale, 2);
            _isManualZoom = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanNext))]
    private void NextPage()
    {

        CurrentPageIndex++;
    }

    [RelayCommand(CanExecute = nameof(CanPrev))]
    private void PrevPage()
    {

        CurrentPageIndex--;

    }

    private bool CanNext()
    {
        return CurrentPageIndex < TotalPage - 1;
    }

    private bool CanPrev()
    {
        return CurrentPageIndex > 0;
    }

    private static BitmapSource RenderPageToMemDC(PdfiumViewer.PdfDocument document, int page, int width, int height, float dpiX = 96, float dpiY = 96)
    {
        using var image = document.Render(page, width, height, dpiX, dpiY, true);
        return BitmapHelper.ToBitmapSource(image);
    }

    #endregion


    #region Paging
    [ObservableProperty]
    private string[] _listFont = ["HELVETICA", "HELVETICA_OBLIQUE", "HELVETICA_BOLD", "HELVETICA_BOLDOBLIQUE", "COURIER", "COURIER_OBLIQUE", "COURIER_BOLD", "COURIER_BOLDOBLIQUE", "TIMES_ROMAN", "TIMES_ITALIC", "TIMES_BOLD"];

    [ObservableProperty]
    private string _selectedFont = "HELVETICA";

    [ObservableProperty]
    private float _fontSize = 11;

    [ObservableProperty]
    private iText.Kernel.Colors.Color _fontColor = ColorConstants.BLACK;

    [ObservableProperty]
    private SolidColorBrush _previewFontBrush = new SolidColorBrush(Colors.Black);

    [ObservableProperty]
    private string _pagingContent = string.Empty;

    public string DefaultPagingContent => _defaultPagingContent;

    public string PreviewPagingText
    {
        get
        {
            var pageNumber = CurrentPageIndex >= 0 ? CurrentPageIndex + 1 : 1;
            var totalPages = TotalPage > 0 ? TotalPage : Math.Max(pageNumber, 1);
            return BuildPagingText(pageNumber, totalPages);
        }
    }

    partial void OnPagingContentChanged(string value)
    {
        OnPropertyChanged(nameof(PreviewPagingText));
    }

    partial void OnSelectPathChanged(string? value)
    {
        CancelPreviewAllRendering();
        var hasFile = !string.IsNullOrWhiteSpace(value);
        HasSelectedFile = hasFile;

        if (hasFile)
        {
            OpenedFilePathVisibility = Visibility.Visible;
            PreviewPages.Clear();
            CurrentViewPage = null;
            _isManualZoom = false;
            UpdateZoomCommandStates();
        }
        else
        {
            OpenedFilePathVisibility = Visibility.Collapsed;
            _pdfDocument?.Dispose();
            _pdfDocument = null;
            PreviewPages.Clear();
            CurrentViewPage = null;
            TotalPage = 0;
            CurrentPageIndex = 0;
            ZoomLevel = 1.0;
            _isManualZoom = false;
            UpdateZoomCommandStates();
        }
        FitToScreenCommand.NotifyCanExecuteChanged();
    }

    partial void OnCurrentViewPageChanged(ImageSource? value)
    {
        if (value is BitmapSource bitmap)
        {
            _currentPagePixelWidth = bitmap.PixelWidth;
            _currentPagePixelHeight = bitmap.PixelHeight;
        }
        else
        {
            _currentPagePixelWidth = 0;
            _currentPagePixelHeight = 0;
        }

        UpdateZoomFitIfNeeded();
        FitToScreenCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPagingCommand))]
    private bool _isTopLeft;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPagingCommand))]
    private bool _isTopCenter;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPagingCommand))]
    private bool _isTopRight;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPagingCommand))]
    private bool _isBottomLeft;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPagingCommand))]
    private bool _isBottomCenter = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPagingCommand))]
    private bool _isBottomRight;

    [RelayCommand]
    private void ChangeFontColor(object content)
    {
        switch (content)
        {
            case SolidColorBrush brush:
                UpdateFontColor(brush.Color);
                break;
            case Border border when border.Background is SolidColorBrush borderBrush:
                UpdateFontColor(borderBrush.Color);
                break;
            case System.Windows.Shapes.Rectangle rectangle when rectangle.Fill is SolidColorBrush fill:
                UpdateFontColor(fill.Color);
                break;
            case System.Windows.Media.Color color:
                UpdateFontColor(color);
                break;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddPaging))]
    private async Task AddPaging()
    {
        if (string.IsNullOrEmpty(SelectPath))
        {
            return;
        }

        if (!HasSelectedPositions())
        {
            var dialog = new ContentDialog
            {
                Title = _localizationSet!["MsgSelectPagingPositionTitle"],
                Content = _localizationSet!["MsgSelectPagingPositionContent"],
                CloseButtonText = "OK"
            };

            await _contentDialogService.ShowAsync(dialog, CancellationToken.None);
            return;
        }

        var saveDialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Pdf file|*.pdf",
            FileName = $"{System.IO.Path.GetFileNameWithoutExtension(SelectPath)}_numbered"
        };

        if (saveDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            PdfReader pdfReader;

            if (string.IsNullOrEmpty(MasterPassword))
            {
                pdfReader = new PdfReader(SelectPath);
            }
            else
            {
                pdfReader = new PdfReader(SelectPath, new ReaderProperties().SetPassword(Encoding.UTF8.GetBytes(MasterPassword)));
            }

            pdfReader.SetUnethicalReading(true);

            using (pdfReader)
            {
                using var pdfWriter = new PdfWriter(saveDialog.FileName);
                using var pdfDocument = new PdfDocument(pdfReader, pdfWriter);

                var font = CreateSelectedFont();
                var totalPages = pdfDocument.GetNumberOfPages();

                for (int i = 1; i <= totalPages; i++)
                {
                    var page = pdfDocument.GetPage(i);
                    var pageSize = page.GetPageSize();
                    var canvas = new PdfCanvas(page);

                    var pagingText = BuildPagingText(i, totalPages);
                    var textWidth = font.GetWidth(pagingText, FontSize);

                    var topY = pageSize.GetTop() - DefaultMargin;
                    var bottomY = pageSize.GetBottom() + DefaultMargin;
                    var leftX = pageSize.GetLeft() + DefaultMargin;
                    var centerX = pageSize.GetLeft() + (pageSize.GetWidth() - textWidth) / 2f;
                    var rightX = pageSize.GetRight() - DefaultMargin - textWidth;

                    if (IsTopLeft)
                    {
                        DrawText(canvas, font, pagingText, leftX, topY);
                    }

                    if (IsTopCenter)
                    {
                        DrawText(canvas, font, pagingText, centerX, topY);
                    }

                    if (IsTopRight)
                    {
                        DrawText(canvas, font, pagingText, rightX, topY);
                    }

                    if (IsBottomLeft)
                    {
                        DrawText(canvas, font, pagingText, leftX, bottomY);
                    }

                    if (IsBottomCenter)
                    {
                        DrawText(canvas, font, pagingText, centerX, bottomY);
                    }

                    if (IsBottomRight)
                    {
                        DrawText(canvas, font, pagingText, rightX, bottomY);
                    }
                }
            }

            var successDialog = new ContentDialog
            {
                Title = _localizationSet!["MsgPagingSavedTitle"],
                Content = string.Format(_localizationSet!["MsgPagingSavedContent"], Environment.NewLine, saveDialog.FileName),
                CloseButtonText = "OK"
            };

            await _contentDialogService.ShowAsync(successDialog, CancellationToken.None);
        }
        catch (Exception ex)
        {
            var errorDialog = new ContentDialog
            {
                Title = _localizationSet!["SaveError"],
                Content = ex.Message,
                CloseButtonText = "OK"
            };

            await _contentDialogService.ShowAsync(errorDialog, CancellationToken.None);
        }
    }

    private void UpdateFontColor(System.Windows.Media.Color color)
    {
        FontColor = new DeviceRgb(color.R, color.G, color.B);
        PreviewFontBrush = new SolidColorBrush(color);
    }

    private PdfFont CreateSelectedFont()
    {
        if (!FontMapping.TryGetValue(SelectedFont, out var fontName))
        {
            fontName = StandardFonts.HELVETICA;
        }

        return PdfFontFactory.CreateFont(fontName);
    }

    private void DrawText(PdfCanvas canvas, PdfFont font, string text, float x, float y)
    {
        canvas.BeginText()
            .SetFontAndSize(font, FontSize)
            .SetColor(FontColor, true)
            .SetTextMatrix(x, y)
            .ShowText(text)
            .EndText();
    }

    private bool HasSelectedPositions()
    {
        return IsTopLeft || IsTopCenter || IsTopRight || IsBottomLeft || IsBottomCenter || IsBottomRight;
    }

    private bool CanAddPaging()
    {
        return !string.IsNullOrEmpty(SelectPath) && HasSelectedPositions();
    }

    private string BuildPagingText(int pageNumber, int totalPageCount)
    {
        var baseContent = string.IsNullOrWhiteSpace(PagingContent) ? _defaultPagingContent : PagingContent!;

        if (baseContent.Contains("{page}", StringComparison.CurrentCultureIgnoreCase) ||
            baseContent.Contains("{total}", StringComparison.CurrentCultureIgnoreCase))
        {
            return baseContent
                .Replace("{page}", pageNumber.ToString(), StringComparison.CurrentCulture)
                .Replace("{total}", totalPageCount.ToString(), StringComparison.CurrentCulture);
        }

        return $"{baseContent.Trim()} {pageNumber}/{totalPageCount}";
    }

    #endregion
}
