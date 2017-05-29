This tool does some postprocessing on set files exported from MSE. Namely, it normalizes apostrophes, fixes card images so that even cards with special characters work in MSE and also splits DFC images into two individual images (as Cockatrice necessitates).

# How to make a MSE set work in Cockatrice

1) Create a new empty folder.
2) In MSE, File->Export->All card images. Use "{card.name}.jpg" as format. Click OK. Choose the folder you created in step 1.
3) In MSE, File->Export->HTML, choose Cockatrice. Enter your set code, leave Images location empty. OK. Choose the folder you created in step 1.
4) [Download the DFC splitter tool archive](https://github.com/mzabsky/dfc-splitter/archive/v1.0.zip) with the Windows binary and extract it somehwere.
5) Run DfcSplitter.exe, give it full path to the set file you exported in step 2 as a command line argument. Note that it works by changing all the image files and the XML file in-place. If something goes wrong or you need to repeat the process for any reason, you need to delete all files in the folder and repeat steps 2 and 3.
6) Copy all the images to %AppData%/../Local/Cockatrice/Cockatrice/pics/downloadedPics/{your set code}
7) Copy the XML file to %AppData%/../Local/Cockatrice/Cockatrice/customsets
8) Open Cockatrice. You should be able to find your cards now.
