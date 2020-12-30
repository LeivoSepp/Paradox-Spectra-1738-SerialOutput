# Paradox-Spectra-1738-SerialOutput
Paradox Spectra 1738 Serial Output reading

Spectra 1738 serial output is 4 bytes.

<table>
    <tr>
                 <td colspan=2>Byte 1</ td> 
                 <td colspan=2>Byte 2</ td> 
                 <td>Byte 3</ td> 
                 <td>Byte 4</ td> 
   </tr>
    <tr>
                 <td>Hex</ td>    
                 <td>Event</ td>  
                 <td>Hex</ td>    
                 <td>Message</ td>  
                 <td>Hex</ td>    
                 <td>Hex</ td>  
    </tr>
    <tr>
                 <td>0x00</ td>    
                 <td>Zone Closed</ td>  
                 <td></ td>    
                 <td>Zones table</ td>  
                 <td rowspan=12>Time</ td>    
                 <td rowspan=12>Time</ td>  
    </tr>
    <tr>
                 <td>0x04</ td>    
                 <td>Zone Open</ td>  
                 <td></ td>    
                 <td>Zones table</ td>  
    </tr>
    <tr>
                 <td>0x08</ td>    
                 <td>Statuses</ td>  
                 <td></ td>    
                 <td>Statuses table</ td>  
    </tr>
    <tr>
                 <td>0x34</ td>    
                 <td>Arming</ td>  
                 <td></ td>    
                 <td>User #</ td>  
    </tr>
    <tr>
                 <td>0x3C</ td>    
                 <td>Disarming</ td>  
                 <td></ td>    
                 <td>User #</ td>  
    </tr>
    <tr>
                 <td>0x40</ td>    
                 <td>Disarm after Alarm</ td>  
                 <td></ td>    
                 <td>User #</ td>  
    </tr>
    <tr>
                 <td>0x14</ td>    
                 <td>Unknown</ td>  
                 <td></ td>    
                 <td></ td>  
    </tr>
    <tr>
                 <td>0x50</ td>    
                 <td>Zone in alarm</ td>  
                 <td></ td>    
                 <td>Zones table</ td>  
    </tr>
    <tr>
                 <td>0x58</ td>    
                 <td>Zone Alarm restore</ td>  
                 <td></ td>    
                 <td>Zones table</ td>  
    </tr>
    <tr>
                 <td>0x70</ td>    
                 <td>Trouble fail</ td>  
                 <td></ td>    
                 <td>Troubles table </ td>  
    </tr>
    <tr>
                 <td>0x74</ td>    
                 <td>Trouble fail restore</ td>  
                 <td></ td>    
                 <td>Troubles table</ td>  
    </tr>
</table>

<table>
    <tr>
                 <td colspan=2>**STATUSES** Byte 2</ td> 
   </tr>
    <tr>
                 <td>Hex</ td>    
                 <td>Status</ td>  
    </tr>
    <tr>
                 <td>0x01</ td>    
                 <td>Zones Open</ td>  
    </tr>
    <tr>
                 <td>0x11</ td>    
                 <td>Zones Closed</ td>  
    </tr>
    <tr>
                 <td>0x21</ td>    
                 <td>Alarm/Bell?</ td>  
    </tr>
    <tr>
                 <td>0x41</ td>    
                 <td>Alarm/Bell?</ td>  
    </tr>
    <tr>
                 <td>0x51</ td>    
                 <td>Alarm occurred during arm</ td>  
    </tr>
    <tr>
                 <td>0x61</ td>    
                 <td>Arm Code entered</ td>  
    </tr>
    <tr>
                 <td>0x71</ td>    
                 <td>Arm Code entered</ td>  
    </tr>
    <tr>
                 <td>0x91</ td>    
                 <td>Disarmed</ td>  
    </tr>
    <tr>
                 <td>0xA1</ td>    
                 <td>Armed</ td>  
    </tr>
    <tr>
                 <td>0xB1</ td>    
                 <td>Entry delay started</ td>  
    </tr>
</table>

<table>
    <tr>
                 <td colspan=2>**TROUBLES** Byte 2</ td> 
   </tr>
    <tr>
                 <td>Hex</ td>    
                 <td>Trouble</ td>  
    </tr>
    <tr>
                 <td>0x21</ td>    
                 <td>Battery</ td>  
    </tr>
    <tr>
                 <td>0x51</ td>    
                 <td>Bell</ td>  
    </tr>
</table>

<table>
    <tr>
                 <td colspan=2>**Zones** Byte 2</ td> 
   </tr>
    <tr>
                 <td>Hex</ td>    
                 <td>Zone</ td>  
    </tr>
    <tr>
                 <td>0x11</ td>    
                 <td>Zone 1</ td>  
    </tr>
    <tr>
                 <td>0x21</ td>    
                 <td>Zone 2</ td>  
    </tr>
    <tr>
                 <td>0x31</ td>    
                 <td>Zone 3</ td>  
    </tr>
    <tr>
                 <td>0x41</ td>    
                 <td>Zone 4</ td>  
    </tr>
    <tr>
                 <td>0x51</ td>    
                 <td>Zone 5</ td>  
    </tr>
    <tr>
                 <td>0x61</ td>    
                 <td>Zone 6</ td>  
    </tr>
    <tr>
                 <td>0x71</ td>    
                 <td>Zone 7</ td>  
    </tr>
    <tr>
                 <td>0x81</ td>    
                 <td>Zone 8</ td>  
    </tr>
</table>