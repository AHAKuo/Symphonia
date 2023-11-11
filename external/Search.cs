using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Symphonia;
using System.Windows;
using Symphonia.scripts;
using System.IO;
using System.Linq;
using static Symphonia.scripts.Defaults;

namespace Symphonia.external
{
    /// <summary>
    /// A system that performs searches for files.
    /// </summary>
    public class Search
    {
        /// <summary>
        /// Encapsulates all the data required to perform a search.
        /// </summary>
        public class SearchData
        {
            public string Path;

            public string SearchFilter;

            public SearchData(string path, string searchFilter)
            {
                Path = path;
                SearchFilter = searchFilter;
            }
        }

        /// <summary>
        /// A query that contains all that is needed to perform a search. As well as events.
        /// </summary>
        public class SearchQuery
        {
            #region Parameters

            private SearchWindow currentWindow;

            /// <summary>
            /// Returns the current search form window and creates an instance of that.
            /// </summary>
            /// <returns></returns>
            private SearchWindow SearchWindow()
            {
                SearchWindow form = new SearchWindow();
                return form;
            }

            /// <summary>
            /// Creates a button and formats it accordingly so it can be added to the flow layout panel.
            /// </summary>
            /// <returns></returns>
            private Button ResultButton()
            {
                Button button = new()
                {
                    Width = currentWindow.FirstButton.Width,
                    Height = currentWindow.FirstButton.Height,
                    Background = currentWindow.FirstButton.Background,
                    Foreground = currentWindow.FirstButton.Foreground,
                    BorderThickness = currentWindow.FirstButton.BorderThickness,
                    FontSize = currentWindow.FirstButton.FontSize,
                    FontFamily = currentWindow.FirstButton.FontFamily,
                };

                return button;
            }

            #endregion

            #region Events
            public delegate void ResultNotFound();
            public event ResultNotFound OnNoResult;

            public delegate void ResultFound();
            public event ResultFound OnResult;

            public delegate void ResultChosen(string s);
            public event ResultChosen OnChooseResult;

            public delegate void SearchFailed();
            public event SearchFailed OnSearchFail;
            #endregion

            #region Methods
            /// <summary>
            /// Perform a search, and when a choice is made, throw choice into textbox.
            /// </summary>
            /// <param name="searchData"></param>
            /// <param name="textBoxEX"></param>
            public void PerformSearch(Action<string> endAction, SearchData searchData, bool ThrowNullMessage = false, bool NoMenuIfOneResult = false, bool ThrowFailMessage = false)
            {
                // first check if query is empty or null or invalid

                if (string.IsNullOrEmpty(searchData.SearchFilter))
                {
                    OnSearchFail?.Invoke();
                    if (ThrowFailMessage)
                    {
                        MessageBox.Show("Invalid query", Defaults.defaultMessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    return;
                }

                // when performing search we need to create a new instance of the search window.

                SearchWindow SearchWindow = new();

                currentWindow = SearchWindow;

                // empty panel
                SearchWindow.StackPanel.Children.Clear();

                List<string> results = new();

                // perform search for song in current path

                // Define the directory where the search will be performed
                string searchDirectory = searchData.Path; // Set your directory path here

                // Split the search filter into individual words
                var searchKeywords = searchData.SearchFilter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                results = supportedFormats.SelectMany(format => Directory.GetFiles(searchDirectory, format, SearchOption.AllDirectories))
                                   .Where(file => searchKeywords.All(keyword => Path.GetFileNameWithoutExtension(file).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
                                   .ToList();

                if (results.Count <= 0)
                {
                    OnNoResult?.Invoke();
                    MessageBox.Show("Couldn't find it!", Defaults.defaultMessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (results == null) // count check
                {
                    SearchWindow.Close();

                    if (ThrowNullMessage)
                    {
                        MessageBox.Show("Null problem.", Defaults.defaultMessageBoxCaption, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }
                else
                {
                    OnResult?.Invoke();
                }

                // then, we need to open the data and create buttons for each data and link them to the choose event.

                StackPanel panelInSearch = SearchWindow.StackPanel;
                List<Button> buttons = new();

                results.ForEach(x =>
                {
                    Button button = ResultButton();
                    button.Content = Path.GetFileNameWithoutExtension(x);
                    button.Click += (sender, e) =>
                    {
                        endAction(x);
                        currentWindow.Close();
                    };
                    buttons.Add(button);
                    panelInSearch.Children.Add(button);
                });

                // finally, show the form as dialog
                if (results.Count == 1 && NoMenuIfOneResult)
                {
                    endAction(results[0]);
                }
                else
                {
                    SearchWindow.ShowDialog();
                }
            }
            #endregion
        }
    }
}
