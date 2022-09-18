using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;

namespace JsonReader
{
    public partial class mainForm : Form
    {
        Dictionary<string, object> _attributes;
        string _filePath = "";
        public mainForm()
        {
            InitializeComponent();
            titleLabel.Text = "";
            errorLabel.Text = "";
        }

        private void loadButton_Click(object sender, EventArgs e)
        {

            //Show "load file" prompt
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Load File";
            openFileDialog.Filter = "Json files|*.json";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _filePath = openFileDialog.FileName;

                //Display filename
                titleLabel.Text = Path.GetFileName(_filePath);

                //Clear error text
                errorLabel.Text = "";

                //Clear all controls from the table in case something was already loaded
                jsonTable.Controls.Clear();

                try
                {
                    var settings = new JsonSerializerSettings
                    {
                        //Deserialise as decimal to avoid potential issues with base-2 floating point imprecision 
                        FloatParseHandling = FloatParseHandling.Decimal
                    };
                    _attributes = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(_filePath), settings);
                }
                catch (Exception ex)
                {
                    errorLabel.Text += "Error: " + ex.Message + "\n";
                    return;
                }

                int i = 0;
                foreach (KeyValuePair<string, object> attr in _attributes)
                {
                    object val = attr.Value;
                    string key = attr.Key;

                    //Create the key label to be displayed in the left column
                    Label attrLabel = new Label();
                    attrLabel.Text = key + ":";
                    attrLabel.Padding = new Padding(5);
                    jsonTable.Controls.Add(attrLabel, 0, i);

                    //Create the value field to be displayed in the right column
                    if (val == null)
                    {
                        Label attrNullLabel = new Label();
                        attrNullLabel.Text = "null";
                        jsonTable.Controls.Add(attrNullLabel, 1, i);
                        i++;
                        continue;
                    }
                    Type type = val.GetType();
                    if (type == typeof(bool))
                    {
                        CheckBox attrCheckbox = new CheckBox();
                        attrCheckbox.Checked = (bool)val;
                        attrCheckbox.Name = key;
                        attrCheckbox.CheckedChanged += new System.EventHandler(attrCheckBox_CheckedChanged);
                        jsonTable.Controls.Add(attrCheckbox, 1, i);
                    }
                    else if (type == typeof(string) || type == typeof(long) || type == typeof(decimal))
                    {
                        TextBox attrTextBox = new TextBox();
                        attrTextBox.Text = val.ToString();
                        attrTextBox.Name = key;
                        jsonTable.Controls.Add(attrTextBox, 1, i);
                        //Use a different text change event depending on type, so that the json type is preserved when the file is saved
                        //And prevent invalid keypresses for integer and decimal values
                        if (type == typeof(string))
                        {
                            attrTextBox.TextChanged += new System.EventHandler(attrTextBox_TextChanged);
                        }
                        if (type == typeof(long))
                        {
                            attrTextBox.TextChanged += new System.EventHandler(attrTextBoxInt_TextChanged);
                            attrTextBox.KeyPress += new KeyPressEventHandler(attrTextBoxInt_KeyPress);
                        }
                        if (type == typeof(decimal))
                        {
                            attrTextBox.TextChanged += new System.EventHandler(attrTextBoxDecimal_TextChanged);
                            attrTextBox.KeyPress += new KeyPressEventHandler(attrTextBoxDecimal_KeyPress);
                        }
                    }
                    //If the attribute type is complex, use a non-modifiable multiline text box
                    else if (type == typeof(Newtonsoft.Json.Linq.JObject) || type == typeof(Newtonsoft.Json.Linq.JArray))
                    {
                        string labeltext = JsonConvert.SerializeObject(val, Formatting.Indented);
                        TextBox attrTextBox = new TextBox();
                        attrTextBox.Text = labeltext;
                        //Height is fixed so that large objects don't dominate the UI
                        attrTextBox.Height = 60; 
                        attrTextBox.Multiline = true;
                        attrTextBox.ScrollBars = ScrollBars.Both;
                        attrTextBox.KeyPress += new KeyPressEventHandler(attrTextBoxReadOnly_KeyPress);
                        jsonTable.Controls.Add(attrTextBox, 1, i);
                    }
                    //The following code should never be reached
                    //If it is, there is a case the system is not handling correctly
                    else
                    {
                        errorLabel.Text += "Error: json file contained object of unexpected type " + type.Name + "\n";
                    }

                    i++;
                }
            }

        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            //Show "save file" prompt
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Save As...";
            saveFileDialog.Filter = "Json files|*.json";
            saveFileDialog.FileName = _filePath;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                _filePath = saveFileDialog.FileName;
                titleLabel.Text = Path.GetFileName(_filePath);
                if (_attributes == null)
                {
                    //Case where no file was loaded
                    File.WriteAllText(_filePath, "{\n}");
                } 
                else
                {
                    File.WriteAllText(_filePath, JsonConvert.SerializeObject(_attributes, Formatting.Indented));
                }
            }
        }

        private void attrTextBoxInt_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Prevent the keypress if the input is not a digit or control character
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void attrTextBoxDecimal_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Prevent the keypress if the input is not a digit or control character, or if it is a decimal point and there is already one present
            if (!char.IsControl(e.KeyChar)
                && !char.IsDigit(e.KeyChar)
                && (e.KeyChar != '.' || ((TextBox)sender).Text.Contains(".")) 
                )
            {
                e.Handled = true;
            }
        }

        private void attrTextBoxReadOnly_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }


        //When any modifiable field is changed, the underlying attribute dictionary is updated to match
        private void attrCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox attrCheckBox = (CheckBox)sender;
            _attributes[attrCheckBox.Name] = attrCheckBox.Checked;
        }

        private void attrTextBox_TextChanged(object sender, EventArgs e)
        {
            TextBox attrTextBox = (TextBox)sender;
            _attributes[attrTextBox.Name] = attrTextBox.Text;
        }

        private void attrTextBoxInt_TextChanged(object sender, EventArgs e)
        {
            TextBox attrTextBox = (TextBox)sender;
            _attributes[attrTextBox.Name] = Convert.ToInt64(attrTextBox.Text);
        }

        private void attrTextBoxDecimal_TextChanged(object sender, EventArgs e)
        {
            TextBox attrTextBox = (TextBox)sender;
            _attributes[attrTextBox.Name] = Convert.ToDecimal(attrTextBox.Text);
        }
    }
}
