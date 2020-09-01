# TelepromptEditor

This is a simple [Windows
application](https://docs.microsoft.com/en-us/dotnet/framework/wpf/introduction-to-wpf) that is
designed to edit SRT files containing the transcript of a video.

Simply set a Video URL in the Video text box and the location of the SRT file in the "SRT File"
text box and the transcript will automatically sync as the video plays or as you move the slider.

Click on the SRT list entry to edit the contents. When you are done click the save button on the
toolbar to save the updated SRT file.

It is assumed that an initial SRT file has been created from automatic transcription and you are
verifying it for correctness, fixing a few typos here and there as the video plays.

![](screenshot.png)

## Installation

To install the app follow this [Clickonce Install
Link](http://lovettsoftware.com/downloads/TelepromptEditor/setup.exe).

Your web browser will download this file which you can then open. You should see the following
dialog:

![install](install.png)

Please be sure to check the "Publisher" is listed as "Chris Lovett".

## SRT File Format

See [example.srt](https://github.com/lovettchris/TelepromptEditor/blob/master/example.srt) for an
example SRT file. The format is very simple:

1. An index starting with 1 for each record.
2. The time range for the snippet
3. The text transcribed during this time range.
4. A blank line.

## Questions

For questions and issues please use the [github issues
list](https://github.com/lovettchris/TelepromptEditor).
