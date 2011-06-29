<?php  
/**
 * Upgrade file to handle the module database upgrade
 * 
 * @package		UEDWS
 * @subpackage	report_uedws
 * @version		2010.0
 * @copyright 	Learning Distance Unit {@link http://ued.ipleiria.pt}, Polytechnic Institute of Leiria
 * @author		Cláudio Esperança <cesperanc@gmail.com>
 * @license 	GNU GPL v3 or later {@link http://www.gnu.org/copyleft/gpl.html}
 */
function xmldb_report_uedws_upgrade($oldversion=0) {
    global $CFG, $THEME, $db;

    $result = true;

    if ($result && $oldversion < 2010101400) {
		$table = new XMLDBTable('uedws_authentication');
		drop_table($table, true, false);
		
		$table->addFieldInfo('id', XMLDB_TYPE_INTEGER, '20', XMLDB_UNSIGNED, XMLDB_NOTNULL, XMLDB_SEQUENCE, null, null, null);
		$table->addFieldInfo('userid', XMLDB_TYPE_INTEGER, '20', XMLDB_UNSIGNED, XMLDB_NOTNULL, null, null, null, null);
		$table->addFieldInfo('username', XMLDB_TYPE_CHAR, '255', null, XMLDB_NOTNULL, null, null, null, null);
		$table->addFieldInfo('autorizationkey', XMLDB_TYPE_CHAR, '255', null, XMLDB_NOTNULL, null, null, null, null);
		$table->addFieldInfo('deviceidentification', XMLDB_TYPE_CHAR, '255', null, null, null, null, null, null);
		$table->addFieldInfo('creationdate', XMLDB_TYPE_CHAR, '255', null, null, null, null, null, null);
		$table->addFieldInfo('lastaccess', XMLDB_TYPE_CHAR, '255', null, null, null, null, null, null);
		$table->addFieldInfo('accesscount', XMLDB_TYPE_INTEGER, '20', XMLDB_UNSIGNED, null, null, null, null, '0');
		$table->addKeyInfo('pk_id', XMLDB_KEY_PRIMARY, array('id'));
		
		$result = $result && create_table($table);
    }

    return $result;
}
