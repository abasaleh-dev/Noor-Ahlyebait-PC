   M S B A M L   `   `   `  ����  NoorAhlulBayt.Browser1    +NoorAhlulBayt.Browser.PinVerificationDialog   5   	   W SPresentationCore, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=9.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   _ [MaterialDesignThemes.Wpf, Version=5.2.1.0, Culture=neutral, PublicKeyToken=df2a72020bd7962aFmaterialDesign1http://materialdesigninxaml.net/winfx/xaml/themes  5   	     � Title$  PIN Verification��5   	   $	��300��5	   	   $	��400��6    � WindowStartupLocation$ CenterOwner=�5
   	    � WindowStyle$
 None=�5   	    � AllowsTransparency$
 True��5   	   
������  � 
ResizeMode$ NoResize=�5   	    � 	Resources 5         IslamicGreenBrush �� Color  IslamicDarkGreenBrush  IslamicLightGreenBrush  IslamicGoldBrush  IslamicDarkBrush  IslamicLightBrush  IslamicButtonStyle �� 
TargetType  MaterialDesignRaisedButton �� BasedOn	 �� Property
 �� Value  IslamicPasswordBoxStyle %	 !MaterialDesignOutlinedPasswordBox)  #MaterialDesignThemes.Wpf.HintAssist 
 WindowControlCloseButton �� 
TargetType �� Triggers P� Property P� Value��%  &	        &	 !     &	 B     &	 c     &	 �     &	 �     &	 �     7  7   7  &	 �    7 	 7   7   &	
     �� 5   
   $ #2E7D32��64   �� 5   
   $ #1B5E20��68   �� 5   
   $ #4CAF50��69   �� 5   
   $ #FFD700��63   �� 5   
   $ #212121��63   �� 5   
   $ #F5F5F5��64   �� 5   
   " ��6+   8   .�5      �� 	 � ��8
  6+   �� 5      	 � ��

 �����6+   �� 5      	 � ��8
  6,   �� 5      	 � ��$
 4q�6'   �� 5      	 � ��$

 12,8q�6(   �� 5       	 � ��$
 80��6)   5!      �� 5$   
   " G�60   8   .�5%      �� 	 �@ 
Foreground8
  6E   �� 5&      	 � ��8
  6,   �� 5'      	 � ��$	
 0,8q�6'   �� 5(      	 � ��$
 14(�6)   5)      �� 5,   
   " ��61   .�5-      �� 	 � ��

 ���� 6+   �� 5.      	 � ��

 �����6+   �� 5/      	 � ��$
 0q�60   �� 50      	 � ��$
 32��6&   �� 51      	 � ��$
 32��6'   �� 52      	 � ��$
 10(�6)   �� 53      	 � ��
 54      �� 55      " ��6&   .R�56      �� #��z��6!   ._�57      �� $��Center=�6/   $��Center=�6L   58      59      5;       5<      P� 5=       �@J�IsMouseOver$
 True��61   . �5>      �� 	 � ��
 �#E8112363   5?      5A      5B      .��5E      �� #��[ 6    �� CornerRadius$ 8��6=   ���#E0E0E0$��1q�6d   ._�5F   
   �  � RowDefinitions 5G      �� 5H      $
��Auto �6    �� 5I      $��* �6    �� 5J      $
��Auto �6    .+�5N      �� -   5        $��0��5N      #��[ 6"   $ 8,8,0,0��6V   ._�5P      � $��32��6    � ColumnDefinitions 5Q      �� 5R      $
��Auto �6+   �� 5S      $��* �6+   �� 5T      $
��Auto �6+   .+�5X      �� $��0��6!    �� Orientation$ 
Horizontal=�61   $��Center=�6J   $
��12,0q�6e   .+�5Y      '  !MaterialDesignThemes.Wpf.PackIcon     Kind$
 Lock=�62   
�������$��16��6Q   $��16��6\   $��Center=�6h   �� 5Z      $��PIN Verification��6$   
�������$��12(�6O   $��Center=�5[   $   $��8,0,0,0q�6?   $��Medium"�6P   5\      �� 5_      $��2��6!   $ 
Horizontal=�61   .+�5`      �� -   5         ��Name$ CloseButton��5`   !   #��[
 6P   $	��✕��5a   !    � ToolTip$ Close��6-   5b      5c      5d      �� 5g      $��1��6   $��32q�6&   $��Center=�62   .+�5i        $ 
ShieldLock=�6*   $��48��6<   $��48��6G   #��[  5j   (   $��Center=�5k   (   $��0,0,0,16q�6E   �� 5n      -   5         ��Name$ MessageTextBlock��5n      $/��)Please enter your PIN to access settings:��66   $��14(�5o       �� TextAlignment$ Center=�6(   $��0,0,0,16q�6?   #��[ 5p      $
��Wrap=�6I   G� 5s      -   5         G�Name$ PinPasswordBox��5s      #��[ 66      Hint$ 	Enter PIN��5t       G� 	MaxLength$ 20��5u   =   �� 5x      -   5        $ ErrorTextBlock��5x      $�� ��64   $��12(�6<   
���  ��$ Center=�5y   +   $��0,8,0,0q�6B   $y�	Collapsed=�5z      5{      �� 5~      $��2��6   #��[ 6"   ���#E0E0E0$��0,1,0,0q�5   +    �� Padding$ 20,12q�6E   ._�5�      �� $ 
Horizontal=�6   $��Right=�66   .+�5�      �� -   5        $ CancelButton��5�      $��Cancel��63   #��[ 6D   
�������$��0,0,8,0q�5�   B   �� 5�      -   5        $ VerifyButton��5�      $��Verify��63   #��[ 6D    �� 	IsDefault$
 True��5�   0   5�      5�      5�      5�      5�      