using System.Windows;
using System.Windows.Media;
using ScreenCut.Annotations;

namespace ScreenCut.Tests.Annotations;

public class AnnotationDocumentTests
{
    [Fact]
    public void Add_ThenUndo_RemovesItem()
    {
        var document = new AnnotationDocument();
        document.Add(CreateRectangle());

        Assert.True(document.Undo());

        Assert.Empty(document.Items);
        Assert.True(document.CanRedo);
    }

    [Fact]
    public void Undo_ThenRedo_RestoresItem()
    {
        var document = new AnnotationDocument();
        document.Add(CreateRectangle());
        document.Undo();

        Assert.True(document.Redo());

        Assert.Single(document.Items);
    }

    [Fact]
    public void NewChange_AfterUndo_ClearsRedo()
    {
        var document = new AnnotationDocument();
        document.Add(CreateRectangle());
        document.Undo();

        document.Add(CreateRectangle());

        Assert.False(document.CanRedo);
    }

    [Fact]
    public void Undo_AfterMove_RestoresOriginalPosition()
    {
        var document = new AnnotationDocument();
        var number = new NumberAnnotation(new Point(10, 10), 1, Colors.Red, 12);
        document.Add(number);

        document.RecordChange();
        number.Offset(new Vector(50, 0));
        document.Undo();

        var restored = Assert.IsType<NumberAnnotation>(Assert.Single(document.Items));
        Assert.Equal(new Point(10, 10), restored.Center);
    }

    [Fact]
    public void CancelChange_RevertsWithoutRedo()
    {
        var document = new AnnotationDocument();
        var number = new NumberAnnotation(new Point(10, 10), 1, Colors.Red, 12);
        document.Add(number);

        document.RecordChange();
        document.RemoveWithoutRecording(number);
        document.CancelChange();

        Assert.Single(document.Items);
        Assert.False(document.CanRedo);
    }

    [Fact]
    public void NextNumber_CountsFromHighestExisting()
    {
        var document = new AnnotationDocument();
        Assert.Equal(1, document.NextNumber);

        document.Add(new NumberAnnotation(new Point(0, 0), 1, Colors.Red, 10));
        document.Add(new NumberAnnotation(new Point(0, 0), 4, Colors.Red, 10));

        Assert.Equal(5, document.NextNumber);
    }

    [Fact]
    public void StrokeClone_IsIndependentCopy()
    {
        var stroke = new StrokeAnnotation(new Point(0, 0), Colors.Blue, 2, isHighlighter: false);
        stroke.UpdateDrag(new Point(0, 0), new Point(10, 10), constrain: false);

        var clone = (StrokeAnnotation)stroke.Clone();
        stroke.Offset(new Vector(100, 100));

        Assert.Equal(new Point(0, 0), clone.Points[0]);
    }

    [Fact]
    public void ShapeTinyDrag_IsNotMeaningful()
    {
        var shape = CreateRectangle();
        shape.UpdateDrag(new Point(0, 0), new Point(1, 1), constrain: false);

        Assert.False(shape.IsMeaningful);
    }

    private static ShapeAnnotation CreateRectangle() => new(ShapeKind.Rectangle, new Point(0, 0), Colors.Red, 2);
}
