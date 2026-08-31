using System.Windows;

namespace TurbolandTheme.Wpf.Controls;

/// <summary>
/// Sizes an element in character cells rather than pixels:
/// <c>ctl:CellSize.Rows="8"</c> makes a list box exactly eight text rows tall,
/// <c>ctl:CellSize.Columns="40"</c> makes a field exactly forty characters wide.
/// </summary>
/// <remarks>
/// <para>
/// This exists because XAML has no arithmetic on resource references, so an app laying
/// out on the character grid cannot write "eight rows" - it would have to write the
/// pixel product, which is both a magic number and <em>wrong at every scale but 1x</em>.
/// Worse, hand-multiplied literals drift off the grid: a list box given a height that
/// is not a whole number of rows is structurally incapable of showing a whole number of
/// rows and will always slice the last one.
/// </para>
/// <para>
/// The cell is <b>not square</b> (9 x 16 at 1x), so rows and columns are separate
/// properties reading separate tokens. There is deliberately no single "cells" property.
/// </para>
/// <para>
/// The value tracks the live token rather than sampling it once: setting
/// <see cref="RowsProperty"/> attaches a <c>DynamicResource</c> reference to a private
/// property holding the current cell height, so a later
/// <see cref="TurbolandTheme.Apply(Application, Core.ThemeMode, int?)"/> at a new scale
/// re-runs the multiplication. Sampling once would freeze every element at whatever
/// scale happened to be active when it was loaded.
/// </para>
/// <para>
/// One limit worth knowing, because it is invisible until it bites: an element that is
/// not in a window's tree still resolves the token <em>once</em> (Application.Resources
/// is the fallback lookup scope) but never hears about a later change, since WPF pushes
/// resource invalidation down from the application's own windows. A disconnected
/// element therefore keeps its first size across a re-Apply. In practice this only
/// shows up in tests, where controls get built without a parent.
/// </para>
/// </remarks>
public static class CellSize
{
    /// <summary>Height of the element in character rows.</summary>
    public static readonly DependencyProperty RowsProperty =
        DependencyProperty.RegisterAttached(
            "Rows", typeof(double), typeof(CellSize),
            new FrameworkPropertyMetadata(double.NaN, OnRowsChanged));

    /// <summary>Width of the element in character columns.</summary>
    public static readonly DependencyProperty ColumnsProperty =
        DependencyProperty.RegisterAttached(
            "Columns", typeof(double), typeof(CellSize),
            new FrameworkPropertyMetadata(double.NaN, OnColumnsChanged));

    // Private carriers for the live token values. They are dependency properties
    // purely so SetResourceReference can drive them; nothing outside this class
    // reads them.
    private static readonly DependencyProperty CellHeightProperty =
        DependencyProperty.RegisterAttached(
            "CellHeight", typeof(double), typeof(CellSize),
            new FrameworkPropertyMetadata(double.NaN, OnCellHeightChanged));

    private static readonly DependencyProperty CellWidthProperty =
        DependencyProperty.RegisterAttached(
            "CellWidth", typeof(double), typeof(CellSize),
            new FrameworkPropertyMetadata(double.NaN, OnCellWidthChanged));

    public static void SetRows(DependencyObject element, double value)
        => element.SetValue(RowsProperty, value);

    public static double GetRows(DependencyObject element)
        => (double)element.GetValue(RowsProperty);

    public static void SetColumns(DependencyObject element, double value)
        => element.SetValue(ColumnsProperty, value);

    public static double GetColumns(DependencyObject element)
        => (double)element.GetValue(ColumnsProperty);

    private static void OnRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => Attach(d, CellHeightProperty, "Turboland.Metric.CellHeight", (double)e.NewValue);

    private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => Attach(d, CellWidthProperty, "Turboland.Metric.CellWidth", (double)e.NewValue);

    private static void Attach(
        DependencyObject d, DependencyProperty carrier, string tokenKey, double count)
    {
        if (d is not FrameworkElement element) return;

        if (double.IsNaN(count))
        {
            element.ClearValue(carrier);
            return;
        }

        // Re-establishing the reference on every change is harmless and keeps the
        // "set Rows, get a live value" contract true even if Rows is set twice.
        element.SetResourceReference(carrier, tokenKey);
    }

    private static void OnCellHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => Apply(d, GetRows(d), (double)e.NewValue, FrameworkElement.HeightProperty);

    private static void OnCellWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => Apply(d, GetColumns(d), (double)e.NewValue, FrameworkElement.WidthProperty);

    private static void Apply(DependencyObject d, double count, double cell, DependencyProperty target)
    {
        if (d is not FrameworkElement element) return;

        if (double.IsNaN(count) || double.IsNaN(cell))
            element.ClearValue(target);
        else
            element.SetValue(target, count * cell);
    }
}
