﻿namespace FFMpegWrapper;

public class DataBaseTable
{
    public string ToJson()
    {
        String output = String.Empty;
        Type type = GetType();
        output += "{\n";
        bool firstLine = true;
        foreach (var prop in type.GetProperties())
        {
            if (firstLine)
            {
                firstLine = false;
               
            }
            else
            {
                output += ",";
            }
            var t = prop.GetValue(this);
            output += "\"";
            output += prop.Name;
            output += "\" : \"";
            output += Convert.ToString(t);
            output += "\"\n";
        }

        output += "}\n";
        return output;
    }

    public virtual object instantiateObject(string[] args)
    {
        return this;
    }
    
}