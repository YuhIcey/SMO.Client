using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Documents;
using SMOClient.Utilities;

namespace SMOClient
{
    public partial class TermsOfServiceWindow : Window
    {
        public TermsOfServiceWindow()
        {
            InitializeComponent();
            
            // Change colors to green theme
            this.Background = new SolidColorBrush(Color.FromRgb(15, 26, 15));  // Dark green-gray
            
            // Find all elements and update their colors
            var buttons = this.FindVisualChildren<Button>();
            var textBlocks = this.FindVisualChildren<TextBlock>();
            var textBoxes = this.FindVisualChildren<TextBox>();
            var scrollViewers = this.FindVisualChildren<ScrollViewer>();
            var borders = this.FindVisualChildren<Border>();
            var richTextBoxes = this.FindVisualChildren<RichTextBox>();

            foreach (var button in buttons)
            {
                button.Background = new SolidColorBrush(Color.FromRgb(47, 255, 47));  // Medium green
                button.Foreground = new SolidColorBrush(Color.FromRgb(15, 26, 15));   // Dark green-gray
                button.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 255, 80)); // Light green
            }

            foreach (var textBlock in textBlocks)
            {
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(165, 255, 165)); // Very light green
                textBlock.Background = new SolidColorBrush(Color.FromRgb(31, 42, 31));    // Slightly lighter green-gray
            }

            foreach (var textBox in textBoxes)
            {
                textBox.Foreground = new SolidColorBrush(Color.FromRgb(165, 255, 165)); // Very light green
                textBox.Background = new SolidColorBrush(Color.FromRgb(31, 42, 31));    // Slightly lighter green-gray
                textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(47, 255, 47));  // Medium green
            }

            foreach (var scrollViewer in scrollViewers)
            {
                scrollViewer.Background = new SolidColorBrush(Color.FromRgb(31, 42, 31));    // Slightly lighter green-gray
                scrollViewer.BorderBrush = new SolidColorBrush(Color.FromRgb(47, 255, 47));  // Medium green
            }

            foreach (var border in borders)
            {
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(47, 255, 47));  // Medium green
                border.Background = new SolidColorBrush(Color.FromRgb(31, 42, 31));    // Slightly lighter green-gray
            }

            foreach (var richTextBox in richTextBoxes)
            {
                richTextBox.Foreground = new SolidColorBrush(Color.FromRgb(165, 255, 165)); // Very light green
                richTextBox.Background = new SolidColorBrush(Color.FromRgb(31, 42, 31));    // Slightly lighter green-gray
                richTextBox.BorderBrush = new SolidColorBrush(Color.FromRgb(47, 255, 47));  // Medium green
                
                // Set proper line spacing for terminal-style text
                if (richTextBox.Document.Blocks.FirstBlock is Paragraph paragraph)
                {
                    paragraph.TextAlignment = TextAlignment.Left;
                    paragraph.LineHeight = 1.2; // Slightly increased line height for readability
                    paragraph.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
                    paragraph.TextIndent = 0;
                    paragraph.Margin = new Thickness(0);
                }
            }
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    // Extension method to find all child elements of a specific type
    public static class VisualExtensions
    {
        public static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
} 