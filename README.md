# yzen-import
params:
  --source filename.csv //filename should have .csv extension and a file uses semicolon as a delimiter.

The app reads and processes a source file with exported dataset from a bank. The app finds MCC codes in source and downloads name of the MCC codes from remote web app. MCC codes are used to define a category.

The "mcc-codes.csv" file is a local storage of the MCC codes. The app uses this file as a cache with known MCC values. The app updates this file from the remote web application during processing a source file.