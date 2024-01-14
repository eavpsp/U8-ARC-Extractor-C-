using System;
using System.IO;
using System.Text;

public class U8Extractor
{
   
    string filePath;
    private const uint U8Magic = 0x55AA382D;
    struct NodeData 
    {
        public string NodeName;
        public string ParentName;
    }

    private static uint ReadBigEndianUInt24(BinaryReader reader)
    {
        byte[] buffer = new byte[4]; // Use a 4-byte buffer
        reader.Read(buffer, 1, 3); // Read 3 bytes into the middle of the buffer
        Array.Reverse(buffer);
        return BitConverter.ToUInt32(buffer, 0);
    }

    private static int ReadBigEndianInt32(BinaryReader reader)
    {
        byte[] buffer = reader.ReadBytes(4);
        Array.Reverse(buffer);
        return BitConverter.ToInt32(buffer, 0);
    }

    private static uint ReadBigEndianUInt32(BinaryReader reader)
    {
        byte[] buffer = reader.ReadBytes(4);
        Array.Reverse(buffer);
        return BitConverter.ToUInt32(buffer, 0);
    }

    private static string ReadStringFromPool(FileStream fileStream, uint offset)
    {
        fileStream.Seek(offset, SeekOrigin.Begin);
        StringBuilder stringBuilder = new StringBuilder();

        char currentChar;
        while ((currentChar = (char)fileStream.ReadByte()) != '\0')
        {
            stringBuilder.Append(currentChar);
        }

        return stringBuilder.ToString();
    }
    public static void ExtractU8(string filePath)
    {
        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            BinaryReader reader = new BinaryReader(fileStream);

            // Read the header
            uint magic = ReadBigEndianUInt32(reader);
            if (magic != U8Magic)
            {
                Console.Write("Invalid U8 file. Magic number does not match.");
                Console.Write("Magic Found: " + magic);
                Console.Write("Magic: " + U8Magic);
                return;
            }

            int offsetToFirstNode = ReadBigEndianInt32(reader);
            int sizeOfAllNodes = ReadBigEndianInt32(reader);
            int fileOffsetOfData = ReadBigEndianInt32(reader);
            Console.Write("Offset: " + offsetToFirstNode);
            Console.Write("DataOffset: " + fileOffsetOfData);
            Console.Write("Size: " + sizeOfAllNodes);
            // Skip reserved space
            reader.BaseStream.Seek(0x10, SeekOrigin.Current);

            // Move to the start of the nodes
            reader.BaseStream.Seek(offsetToFirstNode, SeekOrigin.Begin);
            uint stringPoolAddress = 0;
            // Read nodes
     
            byte nodeType = reader.ReadByte();
            uint offsetToStringPool = ReadBigEndianUInt24(reader);
            int fileOrDirectoryOffset = ReadBigEndianInt32(reader);
            int sizeOrParentIndex = ReadBigEndianInt32(reader);

            // Read file or directory name from string pool
             int totalNodes = 0;
            totalNodes = 12 * sizeOrParentIndex;
            int totalNodeCount = sizeOrParentIndex;
            stringPoolAddress = (uint)offsetToFirstNode + (uint)totalNodes;
            Console.Write("String Pool: " + stringPoolAddress);
            Console.Write("Total Node Size: " + totalNodes);
            reader.BaseStream.Seek(offsetToFirstNode + 0xC, SeekOrigin.Begin);
            int index = 1;
            int dirIndex = 0;
            int indexToSkip = 0;

            string curDirectory = "";
            NodeData[] nodes = new NodeData[totalNodes];

            while (reader.BaseStream.Position < stringPoolAddress)
            {
                nodeType = reader.ReadByte();
                offsetToStringPool = ReadBigEndianUInt24(reader);
                fileOrDirectoryOffset = ReadBigEndianInt32(reader);
                sizeOrParentIndex = ReadBigEndianInt32(reader);
                // Read file or directory name from string pool
                string fileName = ReadStringFromPool(fileStream, offsetToStringPool + stringPoolAddress);
                NodeData node = new NodeData();
                node.NodeName = fileName;
                node.ParentName = "";
                nodes[index] = node;
                //move back to origin
                reader.BaseStream.Seek(offsetToFirstNode + 12 * index, SeekOrigin.Begin);
                if (indexToSkip > 0 && indexToSkip == index)
                {
                    dirIndex = 0;
                    curDirectory = "";
                    indexToSkip = 0;
                }
                // Process file or directory
                if (nodeType == 0x00) // File
                {
                    Console.Write("File: "+fileName +" Data_Offset:" + fileOrDirectoryOffset +"Size: " +sizeOrParentIndex);
                    //WriteDataToFile(nodes[]);
                    if (dirIndex > 0)
                    {
                        string parentName = nodes[dirIndex].NodeName;
                        node.ParentName = parentName;
                        Console.Write("Parent Folder: " + parentName);
                        WriteDataToFile(curDirectory + "/" + fileName, reader, fileOrDirectoryOffset, sizeOrParentIndex);
                        reader.BaseStream.Seek(offsetToFirstNode + 12 * index, SeekOrigin.Begin);

                    }
                    else
                    {
                        WriteDataToFile(fileName, reader, fileOrDirectoryOffset, sizeOrParentIndex);

                        reader.BaseStream.Seek(offsetToFirstNode + 12 * index, SeekOrigin.Begin);

                    }
                }
                else if (nodeType == 0x01) // Directory
                {
                    Console.Write("Directory: "+fileName + "Parent Directory Index:" + fileOrDirectoryOffset+ "First Node Index to Skip: " + sizeOrParentIndex);
                    if (dirIndex > 0 )
                    {
                        string parentName = nodes[dirIndex].NodeName;
                        node.ParentName = parentName ;
                        CreateDirectory(node.ParentName + "/" + fileName);
                        curDirectory = node.ParentName + "/" + fileName;
                    }
                    else
                    {

                        CreateDirectory(fileName);
                        curDirectory = curDirectory + "/" + fileName;

                    }
                    dirIndex = index; //set this as the active dir
                    indexToSkip = sizeOrParentIndex; //this file isnt in the folder
                    
                }
                Console.Write("Current Index: " + index +"/" + totalNodeCount);
                index++;
            }
            
           
            
        }
    }

   
    static void WriteDataToFile(string filename, BinaryReader reader, int offset, int size)
    {
        reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        File.WriteAllBytes(filename, reader.ReadBytes(size));
    }
   static void CreateDirectory(string dir)
    {
        Directory.CreateDirectory("/" + dir);
    }
   static void StoreDataInDictionary()
    {

    }
   public static void Main(string[] args)
{
    if (args.Length > 0)
    {
        ExtractU8(args[0]);
    }
}
}
