﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VSFindTool
{
    /// <summary>
    /// Interaction logic for ReplaceForm.xaml
    /// </summary>
    public partial class ReplaceForm : Window
    {
        public ReplaceForm()
        {
            InitializeComponent();
            tbStrReplace.Focus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void tbStrReplace_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                DialogResult = true;
        }
    }
}
