# PdfTextExtraction 

Project is using domain -driven design (DDD) principles to separate concerns and improve maintainability.

## Library (Class Library):

PdfTextExtractor.Core - Contains core extraction logic, interfaces, and models
Reusable by all consumers
References PDF libraries (e.g., iTextSharp, PdfPig, or Tesseract for OCR)
Console Application:

## PdfTextExtractor.Cli - Command-line interface

Executable name: pdftextextractor.exe or pdfextract.exe
References PdfTextExtractor.Core
WPF Application:

## PdfTextExtractor.Wpf - Windows desktop GUI

Executable name: PdfTextExtractor.exe
References PdfTextExtractor.Core