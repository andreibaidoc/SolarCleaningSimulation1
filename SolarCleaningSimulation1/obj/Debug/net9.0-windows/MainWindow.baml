   M S B A M L   `   `   `  ����S  OSolarCleaningSimulation1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullC&clr-namespace:SolarCleaningSimulation1SolarCleaningSimulation1  5   	   )    #SolarCleaningSimulation1.MainWindow   5   	   W SPresentationCore, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   8local&clr-namespace:SolarCleaningSimulation1     5   	   -   5          � Title$$  Solar Panel Cleaning Simulator��5   	   $	��800��60   $
��1400��6=    � Icon$  Resources/program_icon.ico��6J    � WindowStartupLocation$ CenterScreen=�6l   .��5	      �  � ColumnDefinitions 5
   
   �� 5      $��* �6   �� 5      $	��300 �6   .+�5   
   �� 
���   �$��3q�6%    �� CornerRadius$ 5��69   $��30,100,13,30q�6J   ._�5      �� -   5        $��solar_panel_canvas��5      
�������$��7,7,7,7q�6B   $��Stretch=�6S   $��Stretch=�6q   .+�5      �� -   5         ��Name$ animation_canvas��5      
������ 63   5      5      �� 5   
   -   5         ��Name$ title_label1��5      $$��Solar Panel Cleaning Simulator��6&   $
��Left=�6O   $��	30,18,0,0q�6j   $	��Top=�6}   $��Arial)�6�   $��36(�6�   $
��Bold"�6�   �� 5   
   -   5        $ canvas_name_label1��5      $#��Solar Panel Grid - House Roof��6,   $
��Left=�6T   $��	33,74,0,0q�6o   $	��Top=�6�   $��Arial)�6�   $
��Bold"�6�   $��16(�6�   �� 5   
   -   5        $  roof_dimensions_user_label��5      $'��!Enter the dimensions of the roof:��64   $
��Left=�6`   $��6,67,0,0q�6{   $	��Top=�6�   $��Arial)�6�   $
��Bold"�6�   $	��246��6�   $��1��6�   �� 5   
    �� Orientation$ Vertical=�6   $
��Left=�6,   $	��Top=�6G   $��
12,108,0,0q�6_   $��1��6s   .+�5       �� $��Roof Length (m):��6   $��Center=�60   $��
0, 0, 0, 0q�6K   $��11(�6_   �� 5!      -   5         ��Name$ RoofWidthInput��5!      $	��100��6.   $��11(�6:   $��
0, 5, 0, 0q�6H   $
��Left=�6\   �� 5#      $��Roof Width (m):��6   $��Center=�6/   $��
0, 5, 0, 0q�6J   $��11(�6^   �� 5$      -   5        $��RoofLengthInput��5$      $	��100��6-   $��
0, 5, 0, 0q�69   $��11(�6M   $
��Left=�6[   5%      �� 5(   
   -	   5        $% roof_dimensions_user_label_Copy��5(      $/��)Enter the dimensions of the solar panels:��69   $
��Left=�6m   $��	6,200,0,0q�6�   $	��Top=�6�   $��Arial)�6�   $
��Bold"�6�   $	��276��6�   $��1��6�   �� 5)   
   $ Vertical=�6   $
��Left=�6,   $	��Top=�6G   $��
12,235,0,0q�6_   $��1��6s   $	��122��6�   $	��138��6�   .+�5*      �� $��Width (mm):��6   $��Center=�6+   $��
0, 0, 0, 0q�6F   $��11(�6Z   �� 5+      -
   5        $��
WidthInput��5+      $	��100��6(   $��11(�64   $��
0, 5, 0, 0q�6B   $
��Left=�6V   �� 5-      $��Length (mm):��6   $	��100��6,   $��11(�68   $��
0, 5, 0, 0q�6F   $
��Left=�6Z   �� 5.      -   5        $��LengthInput��5.      $	��100��6)   $��11(�65   $��
0, 5, 0, 0q�6C   $
��Left=�6W   �� 5/      $��Panel Inclination (deg.):��6   $	��127��69   $��11(�6E   $��
0, 5, 0, 0q�6S   $
��Left=�6g   �� 50      -   5        $ PanelInclinationInput��50      $	��100��65   $��11(�6A   $��
0, 5, 0, 0q�6O   $
��Left=�6c   53      �� 58   
   $ Vertical=�6   $
��Left=�6,   $	��Top=�6G   $��
11,484,0,0q�6_   $��1��6s   $	��111��6�   $	��120��6�   .+�59      �� -   5        	 ��Name$	 generate_grid_button��59      $��Generate Grid��63   6K   �� 5:      -   5        $	 place_robot_button��5:      $��Place Robot��61   $y�Hidden=�6h   $��0,5,0,0q�6|   �� 5;      -   5        $	 start_simulation_button��5;      $��Start Simulation��66   $y�Hidden=�6w   $��0,5,0,0q�6�   �� 5<      -   5        $	 stop_simulation_button��5<      $��Stop Simulation��65   $y�Hidden=�6t   $��0,5,0,0q�6�   5=      �� 5@   
   -   5        
 ��Name$
 CoveragePathComboBox��5@      $��1��61   $
��Left=�6A   $��150,484,0,0q�6\   $	��Top=�6q   $	��134��5A      $��23��6   $y�Visible=�6#   68   �� 5B   
   -   5        $ dropbox_path_label��5B      $��
Path type:��6,   $
��Left=�6A   $��150,458,0,0q�6\   $	��Top=�6q   $��Arial)�5C      $	��134��6   $��1��6'   $y�Visible=�67   �� 5H   
   $��16,612,16,0q�6   ���	#FFF6F6F6$ 10��6=   $��1q�6O   
���   �$��1��6w   $��92��6�   $	��Top=�6�   ._�5I      �� -   5        $ error_label��5I      $��	Results: ��6)   $��Trebuchet MS)�6=   $��16(�6W   $��5q�6e   $	��260��6q   $��3,6,3,6q�6}   5J      �� 5K   
   $ Vertical=�6   $
��Left=�6,   $	��Top=�6G   $��
11,382,0,0q�6_   $��1��6s   $	��132��6�   $��88��6�   .+�5L      �� $��Animation Parameters:��6   $	��Top=�65   $
��Left=�6M   $��Arial)�6h   $
��Bold"�6{   $��11(�6�   �� 5M      $��Robot Speed (mm/s):��6   $	��Top=�63   $
��Left=�6K   $��Arial)�6f   $��
0, 5, 0, 0q�6y   $��10(�6�   �� 5N      -   5        $ robot_speed_input_mm_s��5N      $	��100��66   $
��Left=�6B   $��10(�6]   �� 5O      $��Speed Multiplier:��6   $��Arial)�61   $��10(�6D   $��
0, 5, 0, 0q�6R   �� 5P      -   5        $ speed_multiplier_input��5P      $	��100��66   $
��Left=�6B   $��10(�6]   $��
0, 5, 0, 0q�6k   5Q      �� 5R   
   $��1��6   $��130,229,0,0q�6    $-��'/Resources/length_width_panel_input.png��65   $
��Fill=�6f   $
��Left=�6u   $	��154��6�   $	��154��6�   $	��Top=�6�   �� 5S   
   $��1��6   $��0,83,7,0q�6    $,��&/Resources/length_width_roof_input.png��62   $
��Fill=�6b   $��Right=�6q   $	��152��6�   $	��152��6�   $	��Top=�6�   �� 5T   
   -   5        $	 saveFilesToCSVButton��5T      $��1��6/   $��Save files to .csv file��6?   $��Center=�6a   $��	0,719,0,0q�6~   $	��266��6�   $
��Bold"�6�   ���	#FFFE6E00$��25��6�   $	��Top=�6�   
�������$��16(�6�   6  5U      5V      