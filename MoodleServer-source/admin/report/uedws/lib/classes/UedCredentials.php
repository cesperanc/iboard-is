<?php
	/**
	 * Login credentials containner
	 * 
	 * @author Cláudio Esperança, Diogo Serra
	 */
	class UedCredentials {
		/** @var string Username */
		public $username;
		/** @var string AutorizationKey */
		public $autorizationKey;
		
		/**
		 * Constructor
		 * @param string $username
		 * @param string $autorizationKey
		 */
		public function __construct($username, $autorizationKey){
			$this->username = $username;
			$this->autorizationKey = $autorizationKey;
		}
	}
