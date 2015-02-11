' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Roslyn.Test.Utilities
Imports VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Public Class AbstractTextViewFilterTests
        <Fact, WorkItem(617826), Trait(Traits.Feature, Traits.Features.Venus), Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub MapPointsInProjection()
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
                        <Document>
                            class C
                            {
                                static void M()
                                {
                                    {|S1:foreach (var x in new int[] { 1, 2, 3 })
                                    $${ |}
                                        Console.Write({|S2:item|});
                                    {|S3:[|}|]|}
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceXml)
                Dim doc = workspace.Documents.Single()
                Dim projected = workspace.CreateProjectionBufferDocument(<text><![CDATA[
@{|S1:|}
    <span>@{|S2:|}</span>
{|S3:|}
<h2>Default</h2>
                                                         ]]></text>.Value.Replace(vbLf, vbCrLf), {doc}, LanguageNames.CSharp)

                Dim matchingSpan = projected.SelectedSpans.Single()
                TestSpan(workspace, projected, projected.CursorPosition.Value, matchingSpan.End)
                TestSpan(workspace, projected, matchingSpan.End, projected.CursorPosition.Value)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub GotoBraceNavigatesToOuterPositionOfMatchingBrace()
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
                        <Document>
                        using System;
                        class C
                        {
                            static void M()
                            {
                                Console.WriteLine[|$$(Increment(5))|];
                            }
                            static int Increment(int n)
                            {
                                return n+1;
                            }
                        }
                        </Document>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceXml)
                Dim doc = workspace.Documents.Single()
                Dim span = doc.SelectedSpans.Single()
                TestSpan(workspace, doc, doc.CursorPosition.Value, span.End, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE))
                TestSpan(workspace, doc, span.End, doc.CursorPosition.Value, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE))
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub GotoBraceFromLeftAndRightOfOpenAndCloseBraces()
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
                        <Document>
                        using System;
                        class C
                        {
                            static void M()
                            {
                                Console.WriteLine(Increment[|$$(5)|]);
                            }
                            static int Increment(int n)
                            {
                                return n+1;
                            }
                        }
                        </Document>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceXml)
                Dim doc = workspace.Documents.Single()
                Dim span = doc.SelectedSpans.Single()
                TestSpan(workspace, doc, span.Start, span.End, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE))
                TestSpan(workspace, doc, span.End, span.Start, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE))
                TestSpan(workspace, doc, span.Start + 1, span.End, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE))
                TestSpan(workspace, doc, span.End - 1, span.Start, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE))
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub GotoBraceExtFindsTheInnerPositionOfCloseBraceAndOuterPositionOfOpenBrace()
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
                        <Document>
                        using System;
                        class C
                        {
                            static void M()
                            {
                                Console.WriteLine[|(Increment(5)|])$$;
                            }
                            static int Increment(int n)
                            {
                                return n+1;
                            }
                        }
                        </Document>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceXml)
                Dim doc = workspace.Documents.Single()
                Dim span = doc.SelectedSpans.Single()
                TestSpan(workspace, doc, caretPosition:=span.Start, startPosition:=span.Start, endPosition:=span.End, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT))
                TestSpan(workspace, doc, caretPosition:=doc.CursorPosition.Value, startPosition:=span.End, endPosition:=span.Start, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT))
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.BraceMatching)>
        Public Sub GotoBraceExtFromLeftAndRightOfOpenAndCloseBraces()
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= LanguageNames.CSharp %> CommonReferences="true">
                        <Document>
                        using System;
                        class C
                        {
                            static void M()
                            {
                                Console.WriteLine(Increment[|(5)|]);
                            }
                            static int Increment(int n)
                            {
                                return n+1;
                            }
                        }
                        </Document>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceXml)
                Dim doc = workspace.Documents.Single()
                Dim span = doc.SelectedSpans.Single()

                ' Test from left and right of Open parentheses
                TestSpan(workspace, doc, caretPosition:=span.Start, startPosition:=span.Start, endPosition:=span.End - 1, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT))
                TestSpan(workspace, doc, caretPosition:=span.Start + 1, startPosition:=span.Start, endPosition:=span.End - 1, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT))

                ' Test from left and right of Close parentheses
                TestSpan(workspace, doc, caretPosition:=span.End, startPosition:=span.End - 1, endPosition:=span.Start, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT))
                TestSpan(workspace, doc, caretPosition:=span.End - 1, startPosition:=span.End - 1, endPosition:=span.Start, commandId:=CUInt(VSConstants.VSStd2KCmdID.GOTOBRACE_EXT))
            End Using
        End Sub

        Private Shared Sub TestSpan(workspace As TestWorkspace, document As TestHostDocument, startPosition As Integer, endPosition As Integer, Optional commandId As UInteger = Nothing)
            Dim braceMatcher = VisualStudioTestExportProvider.ExportProvider.GetExportedValue(Of IBraceMatchingService)()
            Dim initialLine = document.InitialTextSnapshot.GetLineFromPosition(startPosition)
            Dim initialLineNumber = initialLine.LineNumber
            Dim initialIndex = startPosition - initialLine.Start.Position
            Dim spans() = {New VsTextSpan()}
            Assert.Equal(0, AbstractVsTextViewFilter.GetPairExtentsWorker(
                         document.GetTextView(),
                         workspace,
                         braceMatcher,
                         initialLineNumber,
                         initialIndex,
                         spans,
                         commandId,
                         CancellationToken.None))

            ' Note - we only set either the start OR the end to the result, the other gets set to the source.
            Dim resultLine = document.InitialTextSnapshot.GetLineFromPosition(endPosition)
            Dim resultIndex = endPosition - resultLine.Start.Position
            AssertSpansMatch(startPosition, endPosition, initialLineNumber, initialIndex, spans, resultLine, resultIndex)
        End Sub

        Private Shared Sub TestSpan(workspace As TestWorkspace, document As TestHostDocument, caretPosition As Integer, startPosition As Integer, endPosition As Integer, Optional commandId As UInteger = Nothing)
            Dim braceMatcher = VisualStudioTestExportProvider.ExportProvider.GetExportedValue(Of IBraceMatchingService)()
            Dim initialLine = document.InitialTextSnapshot.GetLineFromPosition(caretPosition)
            Dim initialLineNumber = initialLine.LineNumber
            Dim initialIndex = caretPosition - initialLine.Start.Position
            Dim spans() = {New VsTextSpan()}
            Assert.Equal(0, AbstractVsTextViewFilter.GetPairExtentsWorker(
                         document.GetTextView(),
                         workspace,
                         braceMatcher,
                         initialLineNumber,
                         initialIndex,
                         spans,
                         commandId,
                         CancellationToken.None))
            'In extending selection (GotoBraceExt) scenarios we set both start AND end to the result.
            Dim startIndex = startPosition - initialLine.Start.Position
            Dim resultLine = document.InitialTextSnapshot.GetLineFromPosition(endPosition)
            Dim resultIndex = endPosition - resultLine.Start.Position
            AssertSpansMatch(startPosition, endPosition, initialLineNumber, startIndex, spans, resultLine, resultIndex)
        End Sub

        Private Shared Sub AssertSpansMatch(startPosition As Integer, endPosition As Integer, initialLineNumber As Integer, startIndex As Integer, spans() As VsTextSpan, resultLine As Text.ITextSnapshotLine, resultIndex As Integer)
            If endPosition > startPosition Then
                Assert.Equal(initialLineNumber, spans(0).iStartLine)
                Assert.Equal(startIndex, spans(0).iStartIndex)
                Assert.Equal(resultLine.LineNumber, spans(0).iEndLine)
                Assert.Equal(resultIndex, spans(0).iEndIndex)
            Else
                Assert.Equal(resultLine.LineNumber, spans(0).iStartLine)
                Assert.Equal(resultIndex, spans(0).iStartIndex)
                Assert.Equal(initialLineNumber, spans(0).iEndLine)
                Assert.Equal(startIndex, spans(0).iEndIndex)
            End If
        End Sub
    End Class
End Namespace
