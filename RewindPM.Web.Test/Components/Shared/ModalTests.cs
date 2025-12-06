using Bunit;
using Microsoft.AspNetCore.Components;
using RewindPM.Web.Components.Shared;

namespace RewindPM.Web.Test.Components.Shared;

public class ModalTests : Bunit.TestContext
{
    [Fact(DisplayName = "IsVisibleがfalseの場合、モーダルが非表示")]
    public void Modal_IsNotVisible_WhenIsVisibleIsFalse()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, false)
            .Add(p => p.Title, "Test Modal"));

        // Assert
        Assert.Empty(cut.Markup);
    }

    [Fact(DisplayName = "IsVisibleがtrueの場合、モーダルが表示される")]
    public void Modal_IsVisible_WhenIsVisibleIsTrue()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test Modal"));

        // Assert
        Assert.NotEmpty(cut.Markup);
        var overlay = cut.Find(".modal-overlay");
        Assert.NotNull(overlay);
    }

    [Fact(DisplayName = "モーダルにタイトルが表示される")]
    public void Modal_DisplaysTitle()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test Modal Title"));

        // Assert
        var title = cut.Find(".modal-title");
        Assert.Equal("Test Modal Title", title.TextContent);
    }

    [Fact(DisplayName = "モーダルに子コンテンツが表示される")]
    public void Modal_RendersChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test Modal")
            .Add(p => p.ChildContent, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "test-content");
                builder.AddContent(2, "Test Content");
                builder.CloseElement();
            }));

        // Assert
        var content = cut.Find(".test-content");
        Assert.Equal("Test Content", content.TextContent);
    }

    [Fact(DisplayName = "閉じるボタンクリック時にIsVisibleChangedイベントが発火する")]
    public void Modal_InvokesIsVisibleChanged_WhenCloseButtonClicked()
    {
        // Arrange
        var isVisibleChangedInvoked = false;

        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test Modal")
            .Add(p => p.IsVisibleChanged, EventCallback.Factory.Create<bool>(this, (value) =>
            {
                isVisibleChangedInvoked = true;
                Assert.False(value);
            })));

        // Act
        var closeButton = cut.Find(".modal-close-btn");
        closeButton.Click();

        // Assert
        Assert.True(isVisibleChangedInvoked);
    }

    [Fact(DisplayName = "CloseOnOverlayClickがtrueの場合、オーバーレイクリック時にモーダルが閉じる")]
    public void Modal_InvokesIsVisibleChanged_WhenOverlayClicked_AndCloseOnOverlayClickIsTrue()
    {
        // Arrange
        var isVisibleChangedInvoked = false;

        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test Modal")
            .Add(p => p.CloseOnOverlayClick, true)
            .Add(p => p.IsVisibleChanged, EventCallback.Factory.Create<bool>(this, (value) =>
            {
                isVisibleChangedInvoked = true;
                Assert.False(value);
            })));

        // Act
        var overlay = cut.Find(".modal-overlay");
        overlay.Click();

        // Assert
        Assert.True(isVisibleChangedInvoked);
    }

    [Fact(DisplayName = "CloseOnOverlayClickがfalseの場合、オーバーレイクリック時にモーダルが閉じない")]
    public void Modal_DoesNotClose_WhenOverlayClicked_AndCloseOnOverlayClickIsFalse()
    {
        // Arrange
        var isVisibleChangedInvoked = false;

        var cut = RenderComponent<Modal>(parameters => parameters
            .Add(p => p.IsVisible, true)
            .Add(p => p.Title, "Test Modal")
            .Add(p => p.CloseOnOverlayClick, false)
            .Add(p => p.IsVisibleChanged, EventCallback.Factory.Create<bool>(this, (value) =>
            {
                isVisibleChangedInvoked = true;
            })));

        // Act
        var overlay = cut.Find(".modal-overlay");
        overlay.Click();

        // Assert
        Assert.False(isVisibleChangedInvoked);
    }
}
