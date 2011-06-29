<?php
/**
 * Base file for the module that glue all the module components to build the web service functionalities
 * 
 * @package		UEDWS
 * @subpackage	report_uedws
 * @version		2010.0
 * @copyright 	Learning Distance Unit {@link http://ued.ipleiria.pt}, Polytechnic Institute of Leiria
 * @author		Cláudio Esperança <cesperanc@gmail.com>
 * @license 	GNU GPL v3 or later {@link http://www.gnu.org/copyleft/gpl.html}
 */

    // load moodule files
    require_once('../../../config.php');
    require_once($CFG->libdir.'/adminlib.php');
    require_once($CFG->dirroot.'/course/lib.php');
    require_once($CFG->libdir.'/gradelib.php');
    require_once($CFG->dirroot.'/grade/querylib.php');
    
    if (! $site = get_site()) {
	    error("Could not find site-level course");
	}
	//WebService CONFIG
	$UedWsWebserviceRoot = "{$CFG->wwwroot}/{$CFG->admin}/report/uedws/";
	// internate module name
    $modulename = 'moodle/uedws';
    // module library directory
    $UedWsLibDir = dirname(__FILE__).'/lib';
    
    global $HTTP_RAW_POST_DATA;
	
	// Class loader
	function __autoload($className) {
		$classFile = dirname(__FILE__)."/lib/classes/$className.php";
		if(is_readable($classFile)){
			require_once($classFile);
		}
	}
	
    require_once("$UedWsLibDir/UEDWS.php");
    
	$action = optional_param('action', '', PARAM_ACTION);
	switch(strtolower($action)){
		case "view":{
			admin_externalpage_setup('viewuedws');
			admin_externalpage_print_header();
			
			if(!UEDWS::isWebserviceEnabled()):
				print_string('uedws_theuedwsauthenticationpluginisdisabled', 'report_uedws');
			else:
				?><div><?php print_string('uedws_webserviceupandrunningat', 'report_uedws', "<a href=\"$UedWsWebserviceRoot?wsdl\" target=\"_blank\">$UedWsWebserviceRoot?wsdl</a>");?></div><?php
			endif;
			if($authorizations = get_records('uedws_authentication')):
				?>
					<hr />
					<table cellspacing="0" cellpadding="5" summary="" border="1">
						<caption style="font-size: 16px; font-weight: bold;"><?php print_string('uedws_authorizationlist', 'report_uedws'); ?></caption>
						<tr>
							<th><?php print_string('uedws_username', 'report_uedws'); ?></th>
							<th><?php print_string('uedws_autorizationkey', 'report_uedws'); ?></th>
							<th><?php print_string('uedws_deviceidentification', 'report_uedws'); ?></th>
							<th><?php print_string('uedws_creationdate', 'report_uedws'); ?></th>
							<th><?php print_string('uedws_lastaccess', 'report_uedws'); ?></th>
							<th><?php print_string('uedws_accesscount', 'report_uedws'); ?></th>
						</tr>
						<?php 
							foreach($authorizations as $authorization){
								?>
									<tr>
										<td><?php echo($authorization->username); ?></td>
										<td><?php echo($authorization->autorizationkey); ?></td>
										<td><?php echo($authorization->deviceidentification); ?></td>
										<td><?php echo(date("Y-m-d, H:i:s", $authorization->creationdate)); ?></td>
										<td><?php echo(date("Y-m-d, H:i:s", $authorization->lastaccess)); ?></td>
										<td><?php echo($authorization->accesscount); ?></td>
									</tr>
								<?php 
							}
						?>
					</table>
				<?php 
			endif;
			
			admin_externalpage_print_footer();
			exit();
		}
		default:{
			// If we have an WSDL request, output the service definition data
		    if(isset($_REQUEST['wsdl'])){
		    	header ("content-type: text/xml");
				require_once("$UedWsLibDir/wsdl/WsdlDefinition.php");
				require_once("$UedWsLibDir/wsdl/WsdlWriter.php");
				$def = new WsdlDefinition();
				$def->setDefinitionName('UEDWS');
				$def->setClassFileName("$UedWsLibDir/UEDWS.php");
				$def->setNameSpace($UedWsWebserviceRoot);
				$def->setEndPoint($UedWsWebserviceRoot.'index.php');
				$def->setBaseUrl($UedWsWebserviceRoot.'index.php');
				
				$wsdl = new WsdlWriter($def, true,true);
				echo($wsdl->classToWsdl());
		    }elseif(!empty($HTTP_RAW_POST_DATA)) {
				// disable WSDL cache
		    	//ini_set('soap.wsdl_cache_enabled', 0);
				
				//$server = new SoapServer("http://localhost/ued_ws/admin/report/uedws/?wsdl");
				$server = new SoapServer($UedWsWebserviceRoot."?wsdl");
				$server->setClass("UEDWS");
				// handle the user request
				$server->handle();
			}
		}
	}